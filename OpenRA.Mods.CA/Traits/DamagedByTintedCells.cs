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
	[Desc("This actor receives damage when in TintedCell area.")]
	class DamagedByTintedCellsInfo : ConditionalTraitInfo, Requires<HealthInfo>, IRulesetLoaded
	{
		[Desc("Receive damage from the TintedCell layer with this name.")]
		public readonly string LayerName = "radioactivity";

		[Desc("Damage received per level, per DamageInterval. (Damage = CellLevel / DamageLevel * Damage")]
		public readonly int Damage = 500;

		[Desc("How much TintedCell.Level it takes for it to inflict damage X times.")]
		public readonly int DamageLevel = 100;

		[Desc("Delay (in ticks) between receiving damage.")]
		public readonly int DamageInterval = 16;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

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

		public override object Create(ActorInitializer init) { return new DamagedByTintedCells(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			base.RulesetLoaded(rules, info);

			if (DamageLevel == 0)
				throw new YamlException("DamageLevel of DamagedByTintedCells of actor \"" + info.Name + "\" cannot be 0.");

			var layers = rules.Actors["world"].TraitInfos<TintedCellsLayerInfo>()
				.Where(l => l.Name == LayerName);

			if (!layers.Any())
				throw new InvalidOperationException("There is no TintedCellsLayer named \"" + LayerName + "\" to match DamagedByTintedCells of actor \"" + info.Name + "\"");

			if (layers.Count() > 1)
				throw new InvalidOperationException("There are multiple TintedCellsLayers named \""
					+ LayerName + "\" to match DamagedByTintedCells of actor \"" + info.Name + "\"");
		}
	}

	class DamagedByTintedCells : ConditionalTrait<DamagedByTintedCellsInfo>, ITick, ISync
	{
		readonly TintedCellsLayer tcLayer;

		[Sync]
		int damageTicks;

		[Sync]
		int conditionTicks;
		int conditionToken = Actor.InvalidConditionToken;

		public DamagedByTintedCells(Actor self, DamagedByTintedCellsInfo info)
			: base(info)
		{
			tcLayer = self.World.WorldActor.TraitsImplementing<TintedCellsLayer>()
				.Where(l => l.Info.Name == info.LayerName)
				.First();
		}

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
				{
					conditionToken = self.RevokeCondition(conditionToken);
				}
			}
		}

		bool ProcessDamage(Actor self)
		{
			if (--damageTicks > 0)
				return false;

			// Prevents harming cargo.
			if (!self.IsInWorld)
				return false;

			var level = tcLayer.GetLevel(self.Location);
			if (level <= 0)
				return false;

			int dmg = level / Info.DamageLevel * Info.Damage;
			self.InflictDamage(self.World.WorldActor, new Damage(dmg, Info.DamageTypes));
			damageTicks = Info.DamageInterval;

			if (dmg > 0 && Info.FlashDuration > 0)
			{
				self.World.Add(new FlashTargetCA(self, Info.FlashColor, Info.FlashAlpha, Info.FlashDuration, 1, 2, 0));
			}

			return dmg > 0;
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
