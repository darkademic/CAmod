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
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class HuntCA : Activity
	{
		readonly IEnumerable<Actor> targets;
		readonly IMove move;
		int ticks;
		int ticksUntilScan;

		public HuntCA(Actor self)
		{
			ticks = 0;
			ticksUntilScan = self.World.SharedRandom.Next(20, 40);
			move = self.Trait<IMove>();
			var attack = self.TraitsImplementing<AttackBase>().FirstOrDefault(ab => !ab.IsTraitDisabled);

			if (attack == null)
				targets = Enumerable.Empty<Actor>();
			else
				targets = self.World.ActorsHavingTrait<Huntable>().Where(
					a => self != a && !a.IsDead && a.IsInWorld && a.AppearsHostileTo(self)
					&& a.IsTargetableBy(self) && attack.HasAnyValidWeapons(Target.FromActor(a)));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			ticks++;

			if (--ticksUntilScan > 0)
				return false;

			var targetActor = targets.ClosestToWithPathFrom(self);

			if (targetActor != null)
			{
				ticksUntilScan = self.World.SharedRandom.Next(20, 40);

				// We want to keep 2 cells of distance from the target to prevent the pathfinder from thinking the target position is blocked.
				QueueChild(new AttackMoveActivity(self, () => move.MoveWithinRange(Target.FromCell(self.World, targetActor.Location), WDist.FromCells(2))));
				QueueChild(new Wait(25));
			}
			else
				ticksUntilScan = ticks > 375 ? self.World.SharedRandom.Next(250, 500) : self.World.SharedRandom.Next(20, 40);

			return false;
		}
	}
}
