﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI powerdown.")]
	public class PowerDownBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Delay (in ticks) between toggling powerdown.")]
		public readonly int Interval = 150;

		[Desc("Order string that used for powerdown.")]
		public readonly string OrderName = "PowerDown";

		public override object Create(ActorInitializer init) { return new PowerDownBotModuleCA(init.Self, this); }
	}

	public class PowerDownBotModuleCA : ConditionalTrait<PowerDownBotModuleCAInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		PowerManager playerPower;
		int toggleTick;
		readonly Func<Actor, bool> isToggledBuildingsValid;

		// We keep a list to track toggled buildings for performance.
		List<BuildingPowerWrapper> toggledBuildings;

		class BuildingPowerWrapper
		{
			public int ExpectedPowerChanging;
			public Actor Actor;

			public BuildingPowerWrapper(Actor a, int p)
			{
				Actor = a;
				ExpectedPowerChanging = p;
			}
		}

		public PowerDownBotModuleCA(Actor self, PowerDownBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			toggledBuildings = new List<BuildingPowerWrapper>();
			isToggledBuildingsValid = a => a.Owner == self.Owner && !a.IsDead && a.IsInWorld;
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			playerPower = self.TraitOrDefault<PowerManager>();
		}

		protected override void TraitEnabled(Actor self)
		{
			toggleTick = world.LocalRandom.Next(Info.Interval);
			toggledBuildings = new List<BuildingPowerWrapper>();
		}

		int GetTogglePowerChanging(Actor a)
		{
			var powerChangingIfToggled = 0;
			var powerTraits = a.TraitsImplementing<Power>().Where(t => !t.IsTraitDisabled).ToArray();
			if (powerTraits.Length > 0)
			{
				var powerMulTraits = a.TraitsImplementing<PowerMultiplier>().ToArray();
				powerChangingIfToggled = powerTraits.Sum(p => p.Info.Amount) * (powerMulTraits.Sum(p => p.Info.Modifier) - 100) / 100;
				if (powerMulTraits.Any(t => !t.IsTraitDisabled))
					powerChangingIfToggled = -powerChangingIfToggled;
			}

			return powerChangingIfToggled;
		}

		IEnumerable<Actor> GetToggleableBuildings(IBot bot)
		{
			var toggleable = bot.Player.World.ActorsHavingTrait<ToggleConditionOnOrder>(t => !t.IsTraitDisabled && !t.IsTraitPaused)
				.Where(a => a != null && !a.IsDead && a.Owner == player && a.Info.HasTraitInfo<PowerInfo>() && a.Info.HasTraitInfo<PowerMultiplierInfo>() && a.Info.HasTraitInfo<BuildingInfo>());

			return toggleable;
		}

		IEnumerable<BuildingPowerWrapper> GetOnlineBuildings(IBot bot)
		{
			List<BuildingPowerWrapper> toggleableBuildings = new List<BuildingPowerWrapper>();

			foreach (var a in GetToggleableBuildings(bot))
			{
				var powerChanging = GetTogglePowerChanging(a);
				if (powerChanging > 0)
					toggleableBuildings.Add(new BuildingPowerWrapper(a, powerChanging));
			}

			return toggleableBuildings.OrderBy(bpw => bpw.ExpectedPowerChanging);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (toggleTick > 0 || playerPower == null)
			{
				toggleTick--;
				return;
			}

			var power = playerPower.ExcessPower;
			List<Actor> togglingBuildings = new List<Actor>();

			// When there is extra power, check if AI can toggle on
			if (power > 0)
			{
				toggledBuildings = toggledBuildings.Where(bpw => isToggledBuildingsValid(bpw.Actor)).OrderByDescending(bpw => bpw.ExpectedPowerChanging).ToList();
				for (int i = 0; i < toggledBuildings.Count; i++)
				{
					var bpw = toggledBuildings[i];
					if (power + bpw.ExpectedPowerChanging < 0)
						continue;

					togglingBuildings.Add(bpw.Actor);
					power += bpw.ExpectedPowerChanging;
					toggledBuildings.RemoveAt(i);
				}
			}

			// When there is no power, check if AI can toggle off
			// and add those toggled to list for toggling on
			else if (power < 0)
			{
				var buildingsCanBeOff = GetOnlineBuildings(bot);
				foreach (var bpw in buildingsCanBeOff)
				{
					if (power > 0)
						break;

					togglingBuildings.Add(bpw.Actor);
					toggledBuildings.Add(new BuildingPowerWrapper(bpw.Actor, -bpw.ExpectedPowerChanging));
					power += bpw.ExpectedPowerChanging;
				}
			}

			if (togglingBuildings.Count > 0)
			{
				bot.QueueOrder(new Order(Info.OrderName, null, false, groupedActors: togglingBuildings.ToArray()));
			}

			toggleTick = Info.Interval;
		}
	}
}
