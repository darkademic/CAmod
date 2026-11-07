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
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Show an indicator revealing the actor underneath the fog when a GpsRadarProvider is activated.")]
	class GpsRadarDotInfo : ConditionalTraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite used for this actor.")]
		public readonly string Sequence = "idle";

		[PaletteReference(true)]
		public readonly string IndicatorPalettePrefix = "player";

		public readonly bool VisibleInShroud = true;

		public override object Create(ActorInitializer init) { return new GpsRadarDot(this); }
	}

	class GpsRadarDot : ConditionalTrait<GpsRadarDotInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		GpsRadarDotEffect effect;

		readonly HashSet<RangedGpsRadarProvider> rangedObservers = new HashSet<RangedGpsRadarProvider>();

		public GpsRadarDot(GpsRadarDotInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			effect = new GpsRadarDotEffect(self, this);

			base.Created(self);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}

		public void AddRangedObserver(RangedGpsRadarProvider provider)
		{
			rangedObservers.Add(provider);
		}

		public void RemoveRangedObserver(RangedGpsRadarProvider provider)
		{
			rangedObservers.Remove(provider);
		}

		void CleanRangedObservers()
		{
			var rangedObserversToRemove = new List<RangedGpsRadarProvider>();

			foreach (var rangedObserver in rangedObservers)
			{
				if (rangedObserver.Actor.Disposed || rangedObserver.Actor.IsDead)
					rangedObserversToRemove.Add(rangedObserver);
			}

			foreach (var rangedObserver in rangedObserversToRemove)
				rangedObservers.Remove(rangedObserver);
		}

		public bool HasRangedObserver(Actor self, Player p)
		{
			CleanRangedObservers();

			foreach (var rangedObserver in rangedObservers)
			{
				if ((rangedObserver.Actor.Owner == p || p.IsAlliedWith(rangedObserver.Actor.Owner))
					&& rangedObserver.CanObserve(self))
					return true;
			}

			return false;
		}
	}
}
