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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class PaidRearmHostInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new PaidRearmHost(); }
	}

	public class PaidRearmHost : INotifyResupply, ISync
	{
		readonly Dictionary<Actor, Dictionary<AmmoPool, int>> previousAmmoCounts = new();

		void INotifyResupply.BeforeResupply(Actor self, Actor target, ResupplyType types)
		{
			var rearmCost = target.TraitOrDefault<PaidRearmClient>();
			if (rearmCost == null || rearmCost.Info.CostPerAmmo == 0 || !types.HasFlag(ResupplyType.Rearm))
				return;

			if (rearmCost.Info.ValidHostTypes.Any() && !rearmCost.Info.ValidHostTypes.Contains(self.Info.Name))
				return;

			var rearmable = target.TraitOrDefault<Rearmable>();
			if (rearmable == null)
				return;

			previousAmmoCounts[target] = rearmable.RearmableAmmoPools
				.ToDictionary(pool => pool, pool => pool.CurrentAmmoCount);
		}

		void INotifyResupply.ResupplyTick(Actor self, Actor target, ResupplyType types)
		{
			if (!previousAmmoCounts.TryGetValue(target, out var ammoCounts))
				return;

			var rearmCost = target.TraitOrDefault<PaidRearmClient>();
			if (rearmCost == null)
				return;

			var ammoRestored = new Dictionary<AmmoPool, int>();
			long totalCost = 0;
			foreach (var pool in ammoCounts.Keys.ToArray())
			{
				var ammoDelta = pool.CurrentAmmoCount - ammoCounts[pool];
				if (ammoDelta > 0)
				{
					ammoRestored.Add(pool, ammoDelta);
					var ammoCost = (long)ammoDelta * rearmCost.Info.CostPerAmmo;
					totalCost = ammoCost > int.MaxValue - totalCost ? int.MaxValue + 1L : totalCost + ammoCost;
				}

				ammoCounts[pool] = pool.CurrentAmmoCount;
			}

			var paymentTaken = totalCost == 0 || (totalCost <= int.MaxValue
				&& target.Owner.PlayerActor.Trait<PlayerResources>().TakeCash((int)totalCost, true));

			if (!paymentTaken)
			{
				foreach (var ammo in ammoRestored)
				{
					ammo.Key.TakeAmmo(target, ammo.Value);
					ammoCounts[ammo.Key] = ammo.Key.CurrentAmmoCount;
				}
			}

			if (types == ResupplyType.None)
				previousAmmoCounts.Remove(target);
		}
	}
}
