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
using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("A pool of experience that can be added to and taken from.")]
	public class ReclaimableExperiencePoolInfo : TraitInfo
	{
		[Desc("Percentage modifier to apply when adding XP to the pool.")]
		public readonly int Percentage = 100;

		public override object Create(ActorInitializer init) { return new ReclaimableExperiencePool(init, this); }
	}

	public class ReclaimableExperiencePool
	{
		public readonly ReclaimableExperiencePoolInfo Info;
		Dictionary<string, int> xpPool;

		public ReclaimableExperiencePool(ActorInitializer init, ReclaimableExperiencePoolInfo info)
		{
			Info = info;
			xpPool = new Dictionary<string, int>();
		}

		public void AddXpToPool(string type, int amount)
		{
			if (!xpPool.ContainsKey(type))
				xpPool[type] = 0;

			xpPool[type] += Util.ApplyPercentageModifiers(amount, new[] { Info.Percentage });
		}

		public int TakeXpFromPool(string type, int maximumAmount = int.MaxValue)
		{
			if (!xpPool.TryGetValue(type, out var value) || value <= 0)
				return 0;

			int xp = Math.Min(value, maximumAmount);
			xpPool[type] -= xp;
			return xp;
		}
	}
}
