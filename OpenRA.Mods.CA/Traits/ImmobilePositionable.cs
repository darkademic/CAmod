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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Provides a mutable position and facing for actors that are moved externally, such as attachments.")]
	public class ImmobilePositionableInfo : TraitInfo, IPositionableInfo, IFacingInfo
	{
		[Desc("Altitude to maintain while the actor is being repositioned externally.")]
		public readonly WDist Altitude = WDist.Zero;

		[Desc("Facing to use when the actor is created.")]
		public readonly WAngle InitialFacing = WAngle.Zero;

		[Desc("Speed at which the actor turns.")]
		public readonly WAngle TurnSpeed = WAngle.Zero;

		public override object Create(ActorInitializer init) { return new ImmobilePositionable(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new Dictionary<CPos, SubCell>();
		}

		bool IOccupySpaceInfo.SharesCell => false;

		public bool CanEnterCell(World world, Actor self, CPos cell,
			SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return true;
		}

		public WAngle GetInitialFacing() { return InitialFacing; }
	}

	public class ImmobilePositionable : IPositionable, IFacing, ISync, INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		static readonly (CPos, SubCell)[] NoCells = { };

		readonly Actor self;
		readonly ImmobilePositionableInfo info;
		INotifyCenterPositionChanged[] notifyCenterPositionChanged;

		[Sync]
		public WAngle Facing
		{
			get => Orientation.Yaw;
			set => Orientation = Orientation.WithYaw(value);
		}

		public WAngle TurnSpeed => info.TurnSpeed;

		public WRot Orientation { get; private set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft => self.World.Map.CellContaining(CenterPosition);

		public ImmobilePositionable(ActorInitializer init, ImmobilePositionableInfo info)
		{
			self = init.Self;
			this.info = info;

			var locationInit = init.GetOrDefault<LocationInit>();
			var centerPositionInit = init.GetOrDefault<CenterPositionInit>();
			var pos = centerPositionInit?.Value ?? (locationInit != null ? self.World.Map.CenterOfCell(locationInit.Value) : new WPos(0, 0, 0));
			CenterPosition = new WPos(pos.X, pos.Y, info.Altitude.Length);
			Facing = init.GetValue<FacingInit, WAngle>(info.InitialFacing);
		}

		void INotifyCreated.Created(Actor self)
		{
			notifyCenterPositionChanged = self.TraitsImplementing<INotifyCenterPositionChanged>().ToArray();
		}

		public (CPos, SubCell)[] OccupiedCells() { return NoCells; }

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; }
		public bool CanEnterCell(CPos location, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All) { return true; }
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos location, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return SubCell.Invalid;
		}

		public void SetCenterPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(0, 0, CenterPosition.Z));
		}

		public void SetPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, this);

			if (notifyCenterPositionChanged != null)
				foreach (var notify in notifyCenterPositionChanged)
					notify.CenterPositionChanged(self, 0, 0);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
		}
	}
}