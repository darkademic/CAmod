#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class AttackCharged : Attack
	{
		readonly AttackFrontalCharged attackFrontalChargesTrait;
		readonly IFacing facing;

		public AttackCharged(Actor self, in Target target, bool allowMovement, bool forceAttack, Color? targetLineColor = null)
			: base(self, target, allowMovement, forceAttack, targetLineColor)
		{
			attackFrontalChargesTrait = self.TraitsImplementing<AttackFrontalCharged>().ToArray().Where(Exts.IsTraitEnabled).First();
			facing = self.Trait<IFacing>();
		}

		protected override void DoAttack(Actor self, AttackFrontal attack, IEnumerable<Armament> armaments)
		{
			if (!attackFrontalChargesTrait.IsCharged)
				return;

			if (!attack.IsTraitPaused)
				foreach (var a in armaments)
					a.CheckFire(self, facing, target);
		}
	}
}
