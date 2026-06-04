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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants conditions based on the current level of this actor's first available master.")]
	public class InheritsExperienceLevelOfMasterInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Condition to grant at each inherited level.",
			"Key is the required master level.",
			"Value is the condition to grant.")]
		public readonly Dictionary<int, string> Conditions = null;

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions => Conditions.Values;

		[Desc("Number of ticks between master level checks.")]
		public readonly int CheckInterval = 75;

		public override object Create(ActorInitializer init) { return new InheritsExperienceLevelOfMaster(init.Self, this); }
	}

	public class InheritsExperienceLevelOfMaster : INotifyCreated, ITick
	{
		readonly Actor self;
		readonly InheritsExperienceLevelOfMasterInfo info;
		readonly List<(int RequiredLevel, string Condition)> levelConditions = new();
		readonly List<int> conditionTokens = new();

		IEnumerable<MindControllable> mindControllables;
		IEnumerable<SpawnerSlaveBase> spawnerSlaveBases;
		Attachable attachable;
		int currentLevel = -1;
		int ticksUntilCheck;

		public InheritsExperienceLevelOfMaster(Actor self, InheritsExperienceLevelOfMasterInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			mindControllables = self.TraitsImplementing<MindControllable>();
			spawnerSlaveBases = self.TraitsImplementing<SpawnerSlaveBase>();
			attachable = self.TraitOrDefault<Attachable>();
			levelConditions.AddRange(info.Conditions.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)));
			ticksUntilCheck = 0;
		}

		void ITick.Tick(Actor self)
		{
			if (ticksUntilCheck > 0)
			{
				ticksUntilCheck--;
				return;
			}

			ticksUntilCheck = info.CheckInterval - 1;
			UpdateConditions();
		}

		void UpdateConditions()
		{
			var inheritedLevel = FindMaster()?.TraitOrDefault<GainsExperience>()?.Level ?? 0;
			if (inheritedLevel == currentLevel)
				return;

			currentLevel = inheritedLevel;

			for (var i = 0; i < levelConditions.Count; i++)
			{
				var shouldGrant = inheritedLevel >= levelConditions[i].RequiredLevel;
				var hasCondition = i < conditionTokens.Count && conditionTokens[i] != Actor.InvalidConditionToken;

				if (shouldGrant && !hasCondition)
				{
					var token = self.GrantCondition(levelConditions[i].Condition);
					if (conditionTokens.Count <= i)
						conditionTokens.Add(token);
					else
						conditionTokens[i] = token;
				}
				else if (!shouldGrant && hasCondition)
					conditionTokens[i] = self.RevokeCondition(conditionTokens[i]);
			}
		}

		Actor FindMaster()
		{
			foreach (var mindControllable in mindControllables)
				if (mindControllable.Master.HasValue)
					return mindControllable.Master.Value.Actor;

			foreach (var spawnerSlaveBase in spawnerSlaveBases)
				if (spawnerSlaveBase.Master != null)
					return spawnerSlaveBase.Master;

			return attachable?.AttachedToActor;
		}
	}
}
