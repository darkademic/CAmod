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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor provides Radar GPS.")]
	public class RangedGpsRadarProviderInfo : ConditionalTraitInfo
	{
		[Desc("Target types that can be detected. Leave empty to accept all types.")]
		public readonly BitSet<TargetableType> TargetTypes = default(BitSet<TargetableType>);

		[Desc("Reveals within this range.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("The maximum vertical range above terrain to search for actors.",
		"Ignored if 0 (actors are selected regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		public override object Create(ActorInitializer init) { return new RangedGpsRadarProvider(init, this); }
	}

	public class RangedGpsRadarProvider : ConditionalTrait<RangedGpsRadarProviderInfo>, INotifyCenterPositionChanged,
		INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly Actor self;
		readonly HashSet<Actor> observedActors = new HashSet<Actor>();

		public Actor Actor => self;

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		public RangedGpsRadarProvider(ActorInitializer init, RangedGpsRadarProviderInfo info)
			: base(info)
		{
			self = init.Self;
			cachedRange = WDist.Zero;
			cachedVRange = WDist.Zero;
			proximityTrigger = -1;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
			Update();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
			proximityTrigger = -1;

			foreach (var actor in observedActors)
				if (!actor.Disposed)
					actor.TraitOrDefault<GpsRadarDot>()?.RemoveRangedObserver(this);

			observedActors.Clear();
		}

		protected override void TraitEnabled(Actor self)
		{
			desiredRange = Info.Range;
			desiredVRange = Info.MaximumVerticalOffset;
			Update();
		}

		protected override void TraitDisabled(Actor self)
		{
			desiredRange = WDist.Zero;
			desiredVRange = WDist.Zero;
			Update();
		}

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld || IsTraitDisabled)
				return;

			if (self.CenterPosition != cachedPosition)
				Update();
		}

		void Update()
		{
			if (proximityTrigger == -1)
				return;

			cachedPosition = self.CenterPosition;
			cachedRange = desiredRange;
			cachedVRange = desiredVRange;
			self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
		}

		void ActorEntered(Actor a)
		{
			AddRangedObserver(a);
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			// If the produced Actor doesn't occupy space, it can't be in range
			if (produced.OccupiesSpace == null)
				return;

			// Work around for actors produced within the region not triggering until the second tick
			if (IsInRange(produced))
				AddRangedObserver(produced);
		}

		void ActorExited(Actor a)
		{
			RemoveRangedObserver(a);
		}

		void AddRangedObserver(Actor a)
		{
			if (IsTraitDisabled || a.Disposed || self.Disposed)
				return;

			var dotTrait = a.TraitOrDefault<GpsRadarDot>();
			if (dotTrait != null && (Info.TargetTypes.IsEmpty || a.GetEnabledTargetTypes().Overlaps(Info.TargetTypes)))
			{
				observedActors.Add(a);
				dotTrait.AddRangedObserver(this);
			}
		}

		void RemoveRangedObserver(Actor a)
		{
			observedActors.Remove(a);
			if (a.Disposed)
				return;

			var dotTrait = a.TraitOrDefault<GpsRadarDot>();
			if (dotTrait != null)
				dotTrait.RemoveRangedObserver(this);
		}

		bool IsInRange(Actor a)
		{
			return (a.CenterPosition - self.CenterPosition).HorizontalLengthSquared < Info.Range.LengthSquared
				&& (Info.MaximumVerticalOffset.Length == 0
					|| a.World.Map.DistanceAboveTerrain(a.CenterPosition).LengthSquared <= Info.MaximumVerticalOffset.LengthSquared);
		}

		public bool CanObserve(Actor a)
		{
			return self.IsInWorld && !self.IsDead && !self.Disposed && !IsTraitDisabled
				&& !self.Owner.IsAlliedWith(a.Owner) && IsInRange(a);
		}
	}
}
