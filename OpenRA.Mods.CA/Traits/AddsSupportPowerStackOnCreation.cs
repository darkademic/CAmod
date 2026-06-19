#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Adds support power stack entries while the actor is owned and in the world.")]
	public class AddsSupportPowerStackOnCreationInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The order name of the stackable support power.")]
		public readonly string OrderName = null;

		[Desc("Number of stacks granted by this actor.")]
		public readonly int Amount = 1;

		public override object Create(ActorInitializer init) { return new AddsSupportPowerStackOnCreation(this); }
	}

	public class AddsSupportPowerStackOnCreation : ConditionalTrait<AddsSupportPowerStackOnCreationInfo>,
		INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOwnerChanged, INotifyActorDisposing
	{
		readonly List<string> registeredStackIds = new();
		Player registeredOwner;

		public AddsSupportPowerStackOnCreation(AddsSupportPowerStackOnCreationInfo info)
			: base(info)
		{
		}

		protected override void TraitEnabled(Actor self)
		{
			TryRegister(self, self.Owner);
		}

		protected override void TraitDisabled(Actor self)
		{
			Unregister();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			TryRegister(self, self.Owner);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			Unregister();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Unregister();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Unregister();
			TryRegister(self, newOwner);
		}

		void TryRegister(Actor self, Player owner)
		{
			if (IsTraitDisabled || !self.IsInWorld || registeredStackIds.Count > 0)
				return;

			var manager = owner.PlayerActor.TraitOrDefault<StackableSupportPowerManager>();
			if (manager == null)
				return;

			for (var i = 0; i < Info.Amount; i++)
			{
				var stackId = $"{self.ActorID}:{Info.OrderName}:{i}";
				if (manager.AddStack(Info.OrderName, stackId))
					registeredStackIds.Add(stackId);
			}

			if (registeredStackIds.Count > 0)
				registeredOwner = owner;
		}

		void Unregister()
		{
			if (registeredOwner == null || registeredStackIds.Count == 0)
				return;

			var manager = registeredOwner.PlayerActor.TraitOrDefault<StackableSupportPowerManager>();
			if (manager != null)
			{
				foreach (var stackId in registeredStackIds)
					manager.RemoveStack(Info.OrderName, stackId);
			}

			registeredStackIds.Clear();
			registeredOwner = null;
		}
	}
}
