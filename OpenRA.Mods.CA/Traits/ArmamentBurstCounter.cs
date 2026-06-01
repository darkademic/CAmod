#region Copyright & License Information
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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Tracks 1-based shot indexes within a burst for a specific armament.")]
	public class ArmamentBurstCounterInfo : TraitInfo, Requires<ArmamentInfo>
	{
		[Desc("The armament name to track.")]
		public readonly string ArmamentName = "primary";

		public override object Create(ActorInitializer init) { return new ArmamentBurstCounter(init.Self, this); }
	}

	public class ArmamentBurstCounter : INotifyCreated, INotifyAttack, INotifyBurstComplete
	{
		public readonly ArmamentBurstCounterInfo Info;
		readonly Queue<int> pendingBurstCounts = new Queue<int>();
		WeaponInfo weapon;

		public int BurstCount { get; private set; }

		public ArmamentBurstCounter(Actor self, ArmamentBurstCounterInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var armament = self.TraitsImplementing<Armament>().FirstOrDefault(a => a.Info.Name == Info.ArmamentName);
			if (armament == null)
				throw new InvalidOperationException($"`{self.Info.Name}` has `ArmamentBurstCounter` for `{Info.ArmamentName}`, but no matching armament was found.");

			weapon = armament.Weapon;
		}

		public bool MatchesWeapon(WeaponInfo weaponInfo)
		{
			return weapon == weaponInfo;
		}

		public int? ConsumeBurstCount(WeaponInfo weaponInfo)
		{
			if (!MatchesWeapon(weaponInfo) || pendingBurstCounts.Count == 0)
				return null;

			return pendingBurstCounts.Dequeue();
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != Info.ArmamentName)
				return;

			pendingBurstCounts.Enqueue(++BurstCount);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyBurstComplete.FiredBurst(Actor self, in Target target, Armament a)
		{
			if (a.Info.Name != Info.ArmamentName)
				return;

			BurstCount = 0;
		}
	}
}