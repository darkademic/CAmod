#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Reflection;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public interface IStackableSupportPowerInstance
	{
		int StackCount { get; }
		int ReadyStackCount { get; }
		int StackActivationRemainingTicks { get; }
		int StackActivationIntervalTicks { get; }
	}

	public class StackableSupportPowerInstance : SupportPowerInstanceCA, IStackableSupportPowerInstance
	{
		static readonly FieldInfo NotifiedReadyField = typeof(SupportPowerInstance)
			.GetField("notifiedReady", BindingFlags.Instance | BindingFlags.NonPublic);

		readonly SupportPowerInfo info;
		readonly StackableSupportPowerManager stackManager;
		bool enginePrerequisitesAvailable = true;

		public int StackCount => stackManager.GetTotalStackCount(info.OrderName);
		public int StackActivationRemainingTicks => stackManager.GetActivationRemainingTicks(info.OrderName);
		public int ReadyStackCount => stackManager.GetReadyStackCount(info.OrderName);
		public int StackActivationIntervalTicks { get; }

		public StackableSupportPowerInstance(string key, SupportPowerInfo info, SupportPowerManager manager, int stackActivationInterval)
			: base(key, info, manager)
		{
			this.info = info;
			StackActivationIntervalTicks = stackActivationInterval;
			stackManager = manager.Self.TraitOrDefault<StackableSupportPowerManager>()
				?? throw new InvalidOperationException($"{nameof(StackableSupportPowerManager)} is required for {nameof(StackableSupportPowerInstance)}.");
		}

		public override void PrerequisitesAvailable(bool available)
		{
			enginePrerequisitesAvailable = available;
			UpdateAvailability();
		}

		public override void Tick()
		{
			stackManager.Tick(info.OrderName);
			UpdateAvailability();

			var displayRemainingSubTicks = stackManager.GetDisplayRemainingSubTicks(info.OrderName);
			remainingSubTicks = displayRemainingSubTicks > 0 ? displayRemainingSubTicks + 100 : 0;
			base.Tick();
			remainingSubTicks = displayRemainingSubTicks;
		}

		public override void Target()
		{
			if (!Active || !stackManager.CanActivate(info.OrderName))
				return;

			var power = Instances.FirstOrDefault(i => !i.IsTraitPaused);
			if (power == null)
				return;

			if (!HasSufficientFunds(power))
				return;

			Game.Sound.PlayToPlayer(SoundType.UI, Manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(power.Self.World.Map.Rules, power.Self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, power.Self.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(power.Self.Owner, Info.SelectTargetTextNotification);

			power.SelectTarget(power.Self, Key, Manager);
		}

		public override void Activate(Order order)
		{
			UpdateAvailability();
			if (!Active || !stackManager.CanActivate(info.OrderName))
				return;

			var power = Instances.Where(i => !i.IsTraitPaused && !i.IsTraitDisabled)
				.MinByOrDefault(a =>
				{
					if (a.Self.OccupiesSpace == null || order.Target.Type == TargetType.Invalid)
						return 0;

					return (a.Self.CenterPosition - order.Target.CenterPosition).HorizontalLengthSquared;
				});

			if (power == null)
				return;

			if (!HasSufficientFunds(power, true))
				return;

			if (!power.Prepare(power.Self, order, Manager))
				return;

			power.Activate(power.Self, order, Manager);
			stackManager.TryActivate(info.OrderName);
			notifiedCharging = false;
			NotifiedReadyField?.SetValue(this, false);
			remainingSubTicks = stackManager.GetDisplayRemainingSubTicks(info.OrderName);
			UpdateAvailability();
		}

		public override string TooltipTimeTextOverride()
		{
			var totalStacks = stackManager.GetTotalStackCount(info.OrderName);
			if (totalStacks == 0)
				return null;

			var throttleTicks = stackManager.GetActivationRemainingTicks(info.OrderName);
			var remaining = WidgetUtils.FormatTime(RemainingTicks, Manager.Self.World.Timestep);
			var total = WidgetUtils.FormatTime(info.ChargeInterval, Manager.Self.World.Timestep);
			return throttleTicks > 0
				? $"Lockout: {remaining}"
				: $"{remaining} / {total}";
		}

		void UpdateAvailability()
		{
			base.PrerequisitesAvailable(enginePrerequisitesAvailable && stackManager.HasStacks(info.OrderName));
		}

		bool HasSufficientFunds(SupportPower power, bool activate = false)
		{
			if (power.Info.Cost != 0)
			{
				var player = Manager.Self;
				var pr = player.Trait<PlayerResources>();
				if (pr.Cash + pr.Resources < power.Info.Cost)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player.Owner, "Speech",
						pr.Info.InsufficientFundsNotification, player.Owner.Faction.InternalName);
					return false;
				}

				if (activate)
					pr.TakeCash(power.Info.Cost);
			}

			return true;
		}
	}
}
