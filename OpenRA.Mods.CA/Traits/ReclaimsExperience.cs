#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When killed, gives to a reclaimable pool. When created takes from that pool.")]
	public class ReclaimsExperienceInfo : TraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("Pool to use. Uses actor name if not set.")]
		public readonly string Type = null;

		[Desc("Maximum amount of experience that can be reclaimed from the pool on death, as a percentage of unit's value.")]
		public readonly int MaximumReclaimableValuePercentage = 100;

		[Desc("When true, only adds to the pool on death, doesn't reclaim on creation.")]
		public readonly bool OnlyAddToPool = false;

		public override object Create(ActorInitializer init) { return new ReclaimsExperience(init, this); }
	}

	public class ReclaimsExperience : INotifyCreated, INotifyKilled
	{
		public readonly ReclaimsExperienceInfo Info;
		GainsExperience gainsExperienceTrait;
		int maxReclaimableAmount;

		public ReclaimsExperience(ActorInitializer init, ReclaimsExperienceInfo info)
		{
			Info = info;
			gainsExperienceTrait = init.Self.TraitsImplementing<GainsExperience>().First();
			var valuedInfo = init.Self.Info.TraitInfoOrDefault<ValuedInfo>();
			maxReclaimableAmount = valuedInfo != null ? valuedInfo.Cost * info.MaximumReclaimableValuePercentage : 0;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (Info.OnlyAddToPool)
				return;

			var pool = self.Owner.PlayerActor.TraitsImplementing<ReclaimableExperiencePool>().SingleOrDefault();

			if (pool != null)
			{
				var xp = pool.TakeXpFromPool(Info.Type ?? self.Info.Name, maxReclaimableAmount);
				if (xp > 0)
					gainsExperienceTrait.GiveExperience(xp, true);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			var pool = self.Owner.PlayerActor.TraitsImplementing<ReclaimableExperiencePool>().SingleOrDefault();
			if (pool != null)
				pool.AddXpToPool(Info.Type ?? self.Info.Name, gainsExperienceTrait.Experience);
		}
	}
}
