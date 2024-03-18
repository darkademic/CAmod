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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Flags]
	public enum LoadRequirement
	{
		All = 0,
		IdleUnit = 1,
	}

	[Flags]
	public enum TransportOwner
	{
		Self = 0,
		AlliedBot = 1,
		Allies = 2
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI load unit related with Cargo and Passenger traits. Better used with AI unload trait")]
	public class LoadCargoBotModuleInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Actor types that can be targeted for load, must have Cargo.",
			"the boolean describe if this transport need the unit that is no-idle to get inside")]
		public readonly Dictionary<string, LoadRequirement> TransportTypesAndLoadRequirement = default;

		[FieldLoader.Require]
		[Desc("Actor types that used for loading, must have Passenger.")]
		public readonly HashSet<string> PassengerTypes = default;

		[Desc("Actor relationship that can be targeted for load.")]
		public readonly TransportOwner ValidTransportOwner = TransportOwner.Self;

		[Desc("Scan suitable actors and target in this interval.")]
		public readonly int ScanTick = 317;

		[Desc("Don't load passengers to this actor if damage state is worse than this.")]
		public readonly DamageState ValidDamageState = DamageState.Heavy;

		[Desc("Don't load passengers that are further than this distance to this actor.")]
		public readonly WDist MaxDistance = WDist.FromCells(20);

		public override object Create(ActorInitializer init) { return new LoadCargoBotModule(init.Self, this); }
	}

	public class LoadCargoBotModule : ConditionalTrait<LoadCargoBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> invalidTransport;
		int minAssignRoleDelayTicks;

		public LoadCargoBotModule(Actor self, LoadCargoBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			switch (info.ValidTransportOwner)
			{
				case TransportOwner.Self:
					invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
					break;
				case TransportOwner.AlliedBot:
					invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || !a.Owner.IsBot || a.Owner.RelationshipWith(player) != PlayerRelationship.Ally;
					break;
				case TransportOwner.Allies:
					invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || a.Owner.RelationshipWith(player) != PlayerRelationship.Ally;
					break;
			}

			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || (!a.IsIdle && !(a.CurrentActivity is FlyIdle));
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = world.LocalRandom.Next(0, Info.ScanTick);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--minAssignRoleDelayTicks <= 0)
			{
				minAssignRoleDelayTicks = Info.ScanTick;

				var tcs = world.ActorsWithTrait<Cargo>().Where(
				at =>
				{
					var health = at.Actor.TraitOrDefault<IHealth>()?.DamageState;
					return Info.TransportTypesAndLoadRequirement.ContainsKey(at.Actor.Info.Name) && !invalidTransport(at.Actor)
					&& at.Trait.HasSpace(1) && (health == null || health < Info.ValidDamageState);
				}).ToList();

				if (tcs.Count == 0)
					return;

				var tc = tcs.Random(world.LocalRandom);
				var cargo = tc.Trait;
				var transport = tc.Actor;
				var spaceTaken = 0;

				Predicate<Actor> invalidPassenger;
				if (Info.TransportTypesAndLoadRequirement[transport.Info.Name] == LoadRequirement.IdleUnit)
					invalidPassenger = unitCannotBeOrderedOrIsBusy;
				else
					invalidPassenger = unitCannotBeOrdered;

				var passengers = world.ActorsWithTrait<Passenger>().Where(at => Info.PassengerTypes.Contains(at.Actor.Info.Name) && !invalidPassenger(at.Actor) && cargo.HasSpace(at.Trait.Info.Weight) && (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared <= Info.MaxDistance.LengthSquared)
					.OrderBy(at => (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared);

				var orderedActors = new List<Actor>();

				foreach (var p in passengers)
				{
					var mobile = p.Actor.TraitOrDefault<Mobile>();
					if (mobile == null || !mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, p.Actor.Location, transport.Location))
						continue;

					if (cargo.HasSpace(spaceTaken + p.Trait.Info.Weight))
					{
						spaceTaken += p.Trait.Info.Weight;
						orderedActors.Add(p.Actor);
					}

					if (!cargo.HasSpace(spaceTaken + 1))
						break;
				}

				if (orderedActors.Count > 0)
					bot.QueueOrder(new Order("EnterTransport", null, Target.FromActor(transport), false, groupedActors: orderedActors.ToArray()));
			}
		}
	}
}
