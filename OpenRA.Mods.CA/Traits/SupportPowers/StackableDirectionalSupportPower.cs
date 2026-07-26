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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Paratroopers support power with independent cooldown stacks granted by other actors.")]
	public abstract class StackableDirectionalSupportPowerInfo : DirectionalSupportPowerInfo
	{
		[Desc("Minimum delay between activations of ready stacks, measured in ticks.")]
		public readonly int StackActivationInterval = 0;

		[Desc("Maximum number of stacks that can be owned at once. Zero means unlimited.")]
		public readonly int MaxStacks = 0;

		[Desc("Number of stacks granted by this actor.")]
		public readonly int Stacks = 0;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (AllowMultiple)
				throw new YamlException($"Stackable support powers are incompatible with AllowMultiple. Set AllowMultiple to false on {ai.Name}.");
		}

		public override object Create(ActorInitializer init) { return new StackableDirectionalSupportPower(init.Self, this); }
	}

	public class StackableDirectionalSupportPower : DirectionalSupportPower, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyActorDisposing, INotifyOwnerChanged
	{
		readonly StackableDirectionalSupportPowerInfo info;
		readonly List<string> registeredStackIds = new();
		Player registeredOwner;

		public StackableDirectionalSupportPower(Actor self, StackableDirectionalSupportPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			var stackManager = self.TraitOrDefault<StackableSupportPowerManager>() ?? self.Owner.PlayerActor.TraitOrDefault<StackableSupportPowerManager>();
			if (stackManager == null)
				throw new InvalidOperationException($"{nameof(StackableDirectionalSupportPower)} requires {nameof(StackableSupportPowerManager)} on the player actor.");

			stackManager.RegisterPower(info.OrderName, info.ChargeInterval, info.StackActivationInterval, info.MaxStacks, info.OneShot, info.StartFullyCharged);
		}

		public override SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			if (info.Stacks > 0)
				return new StackableSupportPowerInstance(key, info, manager, info.StackActivationInterval);
			else
				return base.CreateInstance(key, manager);
		}

		protected override void TraitEnabled(Actor self)
		{
			AddStacks(self, self.Owner);
		}

		protected override void TraitDisabled(Actor self)
		{
			RemoveStacks();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			AddStacks(self, self.Owner);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			RemoveStacks();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			RemoveStacks();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RemoveStacks();
			AddStacks(self, newOwner);
		}

		void AddStacks(Actor self, Player owner)
		{
			if (IsTraitDisabled || !self.IsInWorld || registeredStackIds.Count > 0)
				return;

			var manager = owner.PlayerActor.TraitOrDefault<StackableSupportPowerManager>();
			if (manager == null)
				return;

			for (var i = 0; i < info.Stacks; i++)
			{
				var stackId = $"{self.ActorID}:{info.OrderName}:{i}";
				if (manager.AddStack(info.OrderName, stackId))
					registeredStackIds.Add(stackId);
			}

			if (registeredStackIds.Count > 0)
				registeredOwner = owner;
		}

		void RemoveStacks()
		{
			if (registeredOwner == null || registeredStackIds.Count == 0)
				return;

			var manager = registeredOwner.PlayerActor.TraitOrDefault<StackableSupportPowerManager>();
			if (manager != null)
			{
				foreach (var stackId in registeredStackIds)
					manager.RemoveStack(info.OrderName, stackId);
			}

			registeredStackIds.Clear();
			registeredOwner = null;
		}
	}
}
