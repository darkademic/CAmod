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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a prerequisite while draining the owner's cash and resources each tick.")]
	public class GrantPrerequisiteResourceDrainPowerInfo : SupportPowerInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Cash and resources to drain each tick while the power is active.")]
		public readonly int ResourceDrain = 1;

		[Desc("Deactivate the power when a resource drain cannot be paid.")]
		public readonly bool DeactivateOnInsufficientFunds = false;

		[FieldLoader.Require]
		[Desc("The prerequisite type that this provides.")]
		public readonly string Prerequisite = null;

		[Desc("Label to display over the support power icon and in its tooltip while the power is active.")]
		public readonly string ActiveText = "ACTIVE";

		[Desc("Label to display over the support power icon and in its tooltip while the power is inactive.")]
		public readonly string InactiveText = "READY";

		[Desc("Color of the support power icon text while the power is active.")]
		public readonly Color ActiveIconOverlayColor = Color.Lime;

		[Desc("Color of the support power icon text while the power is inactive.")]
		public readonly Color InactiveIconOverlayColor = Color.White;

		[Desc("Color of the border drawn around the support power icon while the power is active.")]
		public readonly Color ActiveIconBorderColor = Color.Lime;

		[Desc("Width of the border drawn around the support power icon while the power is active. Set to 0 to disable.")]
		public readonly int ActiveIconBorderWidth = 1;

		[Desc("Number of ticks between active-status color changes. Set to 0 to disable flashing.")]
		public readonly int ActiveStatusBorderInterval = 0;

		public override object Create(ActorInitializer init) { return new GrantPrerequisiteResourceDrainPower(init.Self, this); }

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			yield return Prerequisite;
		}
	}

	public class GrantPrerequisiteResourceDrainPower : SupportPower, ITechTreePrerequisite, INotifyOwnerChanged
	{
		readonly GrantPrerequisiteResourceDrainPowerInfo info;
		readonly string[] prerequisites;
		TechTree techTree;
		bool active;

		public GrantPrerequisiteResourceDrainPower(Actor self, GrantPrerequisiteResourceDrainPowerInfo info)
			: base(self, info)
		{
			this.info = info;
			prerequisites = new[] { info.Prerequisite };
		}

		protected override void Created(Actor self)
		{
			techTree = self.Owner.PlayerActor.Trait<TechTree>();

			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();
			active = false;
		}

		public override SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return new ResourceDrainSupportPowerInstance(key, info, manager);
		}

		public void Activate(Actor self, SupportPowerInstance instance)
		{
			active = true;
			techTree.ActorChanged(self);
		}

		public void Deactivate(Actor self, SupportPowerInstance instance)
		{
			active = false;
			techTree.ActorChanged(self);
		}

		IEnumerable<string> ITechTreePrerequisite.ProvidesPrerequisites => active ? prerequisites : Enumerable.Empty<string>();

		public class ResourceDrainSupportPowerInstance : SupportPowerInstance, IActiveStateSupportPowerInstance
		{
			readonly GrantPrerequisiteResourceDrainPowerInfo info;
			readonly PlayerResources playerResources;
			bool active;

			public bool IsActive => active;
			public bool IsActiveStatusBorderVisible => !active || info.ActiveStatusBorderInterval == 0
				|| Manager.Self.World.WorldTick / info.ActiveStatusBorderInterval % 2 == 0;
			public Color ActiveIconOverlayColor => info.ActiveIconOverlayColor;
			public Color InactiveIconOverlayColor => info.InactiveIconOverlayColor;
			public Color ActiveIconBorderColor => info.ActiveIconBorderColor;
			public int ActiveIconBorderWidth => info.ActiveIconBorderWidth;

			public ResourceDrainSupportPowerInstance(string key, GrantPrerequisiteResourceDrainPowerInfo info, SupportPowerManager manager)
				: base(key, info, manager)
			{
				this.info = info;
				playerResources = manager.Self.Owner.PlayerActor.Trait<PlayerResources>();
			}

			void Deactivate()
			{
				active = false;

				foreach (var power in Instances)
					((GrantPrerequisiteResourceDrainPower)power).Deactivate(power.Self, this);
			}

			public override void Tick()
			{
				base.Tick();

				if (active && !playerResources.TakeCash(info.ResourceDrain, true) && info.DeactivateOnInsufficientFunds)
					Deactivate();
			}

			public override void Target()
			{
				if (Active)
					Manager.Self.World.IssueOrder(new Order(Key, Manager.Self, false) { ExtraData = active ? 0U : 1U });
			}

			public override void Activate(Order order)
			{
				if (active && order.ExtraData == 0)
				{
					Deactivate();
					return;
				}

				if (active || order.ExtraData != 1)
					return;

				var power = Instances.FirstOrDefault(instance => !instance.IsTraitPaused);
				if (power == null || (info.DeactivateOnInsufficientFunds && !playerResources.TakeCash(info.ResourceDrain, true)))
					return;

				active = true;
				power.PlayLaunchSounds();

				foreach (var instance in Instances)
					((GrantPrerequisiteResourceDrainPower)instance).Activate(instance.Self, this);
			}

			public override string IconOverlayTextOverride()
			{
				return Active ? active ? info.ActiveText : info.InactiveText : null;
			}

			public override string TooltipTimeTextOverride()
			{
				return Active ? active ? info.ActiveText : info.InactiveText : null;
			}
		}
	}
}