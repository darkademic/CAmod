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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to any attached actors.")]
	public class GrantConditionToSpawnerSlavesInfo : ConditionalTraitInfo, Requires<SpawnerMasterBaseInfo>
	{
		[FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionToSpawnerSlaves(init.Self, this); }
	}

	public class GrantConditionToSpawnerSlaves : ConditionalTrait<GrantConditionToSpawnerSlavesInfo>
	{
		public readonly new GrantConditionToSpawnerSlavesInfo Info;
		Dictionary<SpawnerSlaveBaseEntry, int> slaveTokens = new();

		SpawnerMasterBase spawnerMaster;

		public GrantConditionToSpawnerSlaves(Actor self, GrantConditionToSpawnerSlavesInfo info)
			: base(info)
		{
			Info = info;
			spawnerMaster = self.Trait<SpawnerMasterBase>();
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var slaveEntry in spawnerMaster.SlaveEntries)
			{
				slaveTokens.TryAdd(slaveEntry, Actor.InvalidConditionToken);

				if (!slaveEntry.IsValid)
					continue;

				slaveTokens[slaveEntry] = slaveEntry.SpawnerSlave.GrantConditionFromMaster(Info.Condition);
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var slaveEntry in spawnerMaster.SlaveEntries)
			{
				if (!slaveEntry.IsValid)
					continue;

				slaveEntry.SpawnerSlave.RevokeConditionFromMaster(slaveTokens[slaveEntry]);
				slaveTokens[slaveEntry] = Actor.InvalidConditionToken;
			}
		}
	}
}
