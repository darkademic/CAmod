#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor receives damage on the specified terrain type.")]
	class DamagedByTerrainCAInfo : ConditionalTraitInfo, Requires<IHealthInfo>, Requires<IOccupySpaceInfo>
	{
		[FieldLoader.Require]
		[Desc("Amount of damage received per DamageInterval ticks.")]
		public readonly int Damage = 0;

		[Desc("Delay between receiving damage.")]
		public readonly int DamageInterval = 0;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[FieldLoader.Require]
		[Desc("Terrain types where the actor will take damage.")]
		public readonly string[] Terrain = Array.Empty<string>();

		[GrantedConditionReference]
		[Desc("Condition to apply on damage.")]
		public readonly string Condition = null;

		[Desc("Condition duration (if 0 will use DamageInterval).")]
		public readonly int ConditionDuration = 0;

		[Desc("Color to flash the actor when damaged.")]
		public readonly Color FlashColor = Color.White;

		[Desc("Alpha value to flash the actor when damaged.")]
		public readonly float FlashAlpha = 0.5f;

		[Desc("Duration of the flash in ticks. 0 for disabled.")]
		public readonly int FlashDuration = 0;

		public override object Create(ActorInitializer init) { return new DamagedByTerrainCA(this); }
	}

	class DamagedByTerrainCA : ConditionalTrait<DamagedByTerrainCAInfo>, ITick, ISync
	{
		[Sync]
		int damageTicks;

		[Sync]
		int conditionTicks;
		int conditionToken = Actor.InvalidConditionToken;

		public DamagedByTerrainCA(DamagedByTerrainCAInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var damaged = ProcessDamage(self);

			if (!string.IsNullOrEmpty(Info.Condition))
			{
				if (damaged)
				{
					if (conditionToken == Actor.InvalidConditionToken)
						conditionToken = self.GrantCondition(Info.Condition);

					conditionTicks = Info.ConditionDuration > 0 ? Info.ConditionDuration : Info.DamageInterval;
				}

				if (conditionToken != Actor.InvalidConditionToken && conditionTicks-- <= 0)
					conditionToken = self.RevokeCondition(conditionToken);
			}
		}

		bool ProcessDamage(Actor self)
		{
			if (--damageTicks > 0)
				return false;

			// Prevents harming cargo.
			if (!self.IsInWorld)
				return false;

			var terrainType = self.World.Map.GetTerrainInfo(self.Location).Type;
			if (!Info.Terrain.Contains(terrainType))
				return false;

			self.InflictDamage(self.World.WorldActor, new Damage(Info.Damage, Info.DamageTypes));
			damageTicks = Info.DamageInterval;

			if (Info.Damage > 0 && Info.FlashDuration > 0)
				self.World.Add(new FlashTargetCA(self, Info.FlashColor, Info.FlashAlpha, Info.FlashDuration, 1, 2, 0));

			return Info.Damage > 0;
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			conditionTicks = 0;
			damageTicks = 0;
		}
	}
}