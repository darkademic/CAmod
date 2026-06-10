#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Applies a condition to actors within a specified range.")]
	public class ProximityExternalConditionCAInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("The range to search for actors.")]
		public readonly WDist Range = WDist.FromCells(3);

		[Desc("The maximum vertical range above terrain to search for actors.",
		"Ignored if 0 (actors are selected regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		[Desc("What player relationships are affected.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Condition is applied permanently to this actor.")]
		public readonly bool AffectsParent = false;

		[Desc("Maximum number of targets to affect. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("Recalculate target distances every tick. If false, only range entry/exit updates the target set.")]
		public readonly bool MaxTargetsRecalculatedForMovement = false;

		public readonly string EnableSound = null;
		public readonly string DisableSound = null;

		public override object Create(ActorInitializer init) { return new ProximityExternalConditionCA(init.Self, this); }
	}

	public class ProximityExternalConditionCA : ConditionalTrait<ProximityExternalConditionCAInfo>,
		ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction, INotifyProximityOwnerChanged
	{
		readonly Actor self;

		readonly Dictionary<Actor, int> tokens = new();
		readonly HashSet<Actor> actorsInRange = new();

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		public ProximityExternalConditionCA(Actor self, ProximityExternalConditionCAInfo info)
			: base(info)
		{
			this.self = self;
			cachedRange = WDist.Zero;
			cachedVRange = WDist.Zero;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
		}

		protected override void TraitEnabled(Actor self)
		{
			Game.Sound.Play(SoundType.World, Info.EnableSound, self.CenterPosition);
			desiredRange = Info.Range;
			desiredVRange = Info.MaximumVerticalOffset;
		}

		protected override void TraitDisabled(Actor self)
		{
			Game.Sound.Play(SoundType.World, Info.DisableSound, self.CenterPosition);
			desiredRange = WDist.Zero;
			desiredVRange = WDist.Zero;
		}

		void ITick.Tick(Actor self)
		{
			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange || desiredVRange != cachedVRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				cachedVRange = desiredVRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
			}

			if (Info.MaxTargets > 0 && Info.MaxTargetsRecalculatedForMovement)
				RefreshLimitedTargets();
		}

		void ActorEntered(Actor a)
		{
			if (a.Disposed || self.Disposed)
				return;

			if (a == self && !Info.AffectsParent)
				return;

			if (tokens.ContainsKey(a))
				return;

			if (Info.MaxTargets > 0)
			{
				actorsInRange.Add(a);
				RefreshLimitedTargets();
				return;
			}

			TryGrantCondition(a);
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			// If the produced Actor doesn't occupy space, it can't be in range.
			if (produced.OccupiesSpace == null)
				return;

			// We don't grant conditions when disabled.
			if (IsTraitDisabled)
				return;

			// Work around for actors produced within the region not triggering until the second tick.
			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				if (Info.MaxTargets > 0)
				{
					actorsInRange.Add(produced);
					RefreshLimitedTargets();
				}
				else
					TryGrantCondition(produced);
			}
		}

		void ActorExited(Actor a)
		{
			if (a.Disposed)
				return;

			if (Info.MaxTargets > 0)
			{
				actorsInRange.Remove(a);
				RefreshLimitedTargets();
				return;
			}

			if (!tokens.TryGetValue(a, out var token))
				return;

			tokens.Remove(a);
			foreach (var external in a.TraitsImplementing<ExternalCondition>())
				if (external.TryRevokeCondition(a, self, token))
					break;
		}

		void INotifyProximityOwnerChanged.OnProximityOwnerChanged(Actor actor, Player oldOwner, Player newOwner)
		{
			// If the Actor doesn't occupy space, it can't be in range.
			if (actor.OccupiesSpace == null)
				return;

			// We don't grant conditions when disabled.
			if (IsTraitDisabled)
				return;

			// Work around for actors changin owner within the region.
			if ((actor.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				if (Info.MaxTargets > 0)
				{
					actorsInRange.Add(actor);
					RefreshLimitedTargets();
					return;
				}

				var hasRelationship = Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(actor.Owner));
				var contains = tokens.TryGetValue(actor, out var token);
				if (hasRelationship && !contains)
				{
					TryGrantCondition(actor);
				}
				else if (!hasRelationship && contains)
				{
					RevokeCondition(actor);
				}
			}
		}

		void RefreshLimitedTargets()
		{
			if (Info.MaxTargets <= 0 || self.Disposed)
				return;

			foreach (var actor in actorsInRange.Where(a => a.Disposed || !a.IsInWorld).ToArray())
				actorsInRange.Remove(actor);

			var selectedActors = actorsInRange
				.Where(a => GetGrantableCondition(a) != null)
				.OrderBy(a => (a.CenterPosition - self.CenterPosition).HorizontalLengthSquared)
				.ThenBy(a => a.ActorID)
				.Take(Info.MaxTargets)
				.ToArray();

			var selectedSet = new HashSet<Actor>(selectedActors);

			foreach (var actor in tokens.Keys.ToArray())
				if (!selectedSet.Contains(actor))
					RevokeCondition(actor);

			foreach (var actor in selectedActors)
				if (!tokens.ContainsKey(actor))
					TryGrantCondition(actor);
		}

		bool TryGrantCondition(Actor actor)
		{
			var external = GetGrantableCondition(actor);
			if (external == null)
				return false;

			tokens[actor] = external.GrantCondition(actor, self);
			return true;
		}

		void RevokeCondition(Actor actor)
		{
			if (!tokens.TryGetValue(actor, out var token))
				return;

			tokens.Remove(actor);
			foreach (var external in actor.TraitsImplementing<ExternalCondition>())
				if (external.TryRevokeCondition(actor, self, token))
					break;
		}

		ExternalCondition GetGrantableCondition(Actor actor)
		{
			if (actor.Disposed || self.Disposed || IsTraitDisabled)
				return null;

			if (actor == self && !Info.AffectsParent)
				return null;

			if (!Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(actor.Owner)))
				return null;

			return actor.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(self));
		}
	}
}
