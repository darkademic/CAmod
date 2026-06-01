

#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Actor can be entered en masse.")]
	public class MassEntersCargoInfo : ConditionalTraitInfo, Requires<PassengerInfo>
	{
		[CursorReference]
		[Desc("Cursor to display when able to enter target actor.")]
		public readonly string EnterCursor = "enter-multi";

		[CursorReference]
		[Desc("Cursor to display when unable to enter target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new MassEntersCargo(init, this); }
	}

	public class MassEntersCargo : ConditionalTrait<MassEntersCargoInfo>, INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice
	{
		private Passenger passenger;

		public int Weight => passenger.Info.Weight;

		public MassEntersCargo(ActorInitializer init, MassEntersCargoInfo info)
			: base(info)
		{ }

		void INotifyCreated.Created(Actor self)
		{
			passenger = self.Trait<Passenger>();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<CargoInfo>(
					"MassEnterTransport",
					10,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					IsCorrectCargoType,
					CanEnter);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "MassEnterTransport")
			{
				var targetActor = target.Type == TargetType.Actor ? target.Actor : null;
				var selectedActors = targetActor == null ? new[] { self } : OrderedPassengersForTarget(self, targetActor, self.World.Selection.Actors).Select(a => a.Actor).ToArray();

				return new Order(order.OrderID, self, target, queued, selectedActors);
			}

			return null;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "MassEnterTransport")
				return null;

			if (order.Target.Type != TargetType.Actor || !CanEnter(order.Target.Actor))
				return null;

			return passenger.Info.Voice;
		}

		bool IsCorrectCargoType(Actor target, TargetModifiers modifiers)
		{
			if (!modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			if (!target.Info.HasTraitInfo<MassEnterableCargoInfo>())
				return false;

			return IsCorrectCargoType(target);
		}

		bool IsCorrectCargoType(Actor target)
		{
			var ci = target.Info.TraitInfo<CargoInfo>();
			return ci.Types.Contains(passenger.Info.CargoType);
		}

		bool CanEnter(Cargo cargo)
		{
			return cargo != null && cargo.HasSpace(passenger.Info.Weight);
		}

		bool CanEnter(Actor target)
		{
			return CanEnter(target.TraitOrDefault<Cargo>());
		}

		public bool IsValidForTarget(Actor target)
		{
			return IsCorrectCargoType(target) && CanEnter(target);
		}

		IEnumerable<TraitPair<MassEntersCargo>> OrderedPassengersForTarget(Actor self, Actor targetActor, IEnumerable<Actor> actors)
		{
			return actors
				.Where(a => a != null
					&& a.Info.HasTraitInfo<MassEntersCargoInfo>()
					&& a.Owner == self.Owner
					&& !a.IsDead)
				.Select(a => new TraitPair<MassEntersCargo>(a, a.TraitOrDefault<MassEntersCargo>()))
				.Where(a => a.Trait != null && a.Trait.IsValidForTarget(targetActor))
				.OrderBy(a => (a.Actor.CenterPosition - targetActor.CenterPosition).LengthSquared)
				.ThenBy(a => a.Actor.ActorID);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "MassEnterTransport")
				return;

			// Enter orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;
			if (targetActor == null || targetActor.IsDead)
				return;

			var selectedWithTrait = OrderedPassengersForTarget(self, targetActor, order.ExtraActors ?? System.Array.Empty<Actor>()).ToArray();
			if (!selectedWithTrait.Any(a => a.Actor == self))
				return;

			// Create a list of available transports
			var availableTransports = self.World.Actors
				.Where(a => a.Info.HasTraitInfo<MassEnterableCargoInfo>()
					&& a.Info.HasTraitInfo<CargoInfo>()
					&& a.Info.Name == targetActor.Info.Name
					&& a.Owner == targetActor.Owner
					&& !a.IsDead
					&& (a.CenterPosition - targetActor.CenterPosition).HorizontalLengthSquared <= WDist.FromCells(10).LengthSquared)
				.Select(a => new TransportInfo {
					Actor = a,
					Cargo = a.Trait<Cargo>(),
					UnallocatedSpace = a.Trait<Cargo>().Info.MaxWeight - a.Trait<Cargo>().Passengers.Sum(p => p.Trait<Passenger>().Info.Weight)
				})
				.Where(t => t.Cargo != null && t.Cargo.HasSpace(1))
				.OrderBy(t => (t.Actor.CenterPosition - targetActor.CenterPosition).LengthSquared)
				.ThenBy(t => t.Actor.ActorID)
				.ToList();

			TransportInfo assignedTransport = null;

			// Allocate passengers to the closest available transport
			foreach (var pair in selectedWithTrait)
			{
				if (availableTransports.Count == 0)
					break;

				var closestTransport = availableTransports
					.Where(t => t.UnallocatedSpace >= pair.Trait.Weight)
					.OrderBy(t => (t.Actor.CenterPosition - pair.Actor.CenterPosition).LengthSquared)
					.ThenBy(t => t.Actor.ActorID)
					.FirstOrDefault();

				if (closestTransport == null)
					continue;

				if (pair.Actor == self)
					assignedTransport = closestTransport;

				if (!closestTransport.Cargo.HasSpace(1))
					availableTransports.Remove(closestTransport);
				else
					closestTransport.UnallocatedSpace -= pair.Trait.Weight;
			}

			if (assignedTransport == null)
				return;

			self.QueueActivity(order.Queued, new MassRideTransport(self, Target.FromActor(assignedTransport.Actor), passenger.Info.TargetLineColor));
			self.ShowTargetLines();
		}
	}

	class TransportInfo
	{
		public Actor Actor { get; set; }
		public Cargo Cargo { get; set; }
		public int UnallocatedSpace { get; set; }
	}
}
