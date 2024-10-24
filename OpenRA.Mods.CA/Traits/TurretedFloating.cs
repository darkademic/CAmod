#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Turret for where the unit is able to move instantly in any direction, to make the turret unaffected by changes in body facing.")]
	public class TurretedFloatingInfo : TurretedInfo
	{
		public override object Create(ActorInitializer init) { return new TurretedFloating(init, this); }
	}

	public class TurretedFloating : Turreted
	{
		WAngle lastBodyFacing;
		IFacing facing;
		bool initialChange = false;

		public TurretedFloating(ActorInitializer init, TurretedFloatingInfo info)
			: base(init, info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			facing = self.TraitOrDefault<IFacing>();
		}

		protected override void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (lastBodyFacing != facing.Facing)
			{
				if (initialChange)
				{
					var facingDiff = lastBodyFacing - facing.Facing;
					LocalOrientation = LocalOrientation.Rotate(new WRot(WAngle.Zero, WAngle.Zero, facingDiff));
				}
				else
				{
					initialChange = true;
				}
			}

			lastBodyFacing = facing.Facing;
			base.Tick(self);
		}
	}
}
