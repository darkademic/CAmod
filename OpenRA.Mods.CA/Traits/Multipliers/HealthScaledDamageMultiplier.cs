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
	[Desc("Modifies the damage taken by the actor according to its maximum health.")]
	public class HealthScaledDamageMultiplierInfo : ConditionalTraitInfo
	{
		[Desc("Minimum health in the range.")]
		public readonly int MinHealth = 5000;

		[Desc("Maximum health in the range.")]
		public readonly int MaxHealth = 30000;

		[Desc("Damage multiplier applied at the minimum health.")]
		public readonly int MinDamageModifier = 50;

		[Desc("Damage multiplier applied at the maximum health.")]
		public readonly int MaxDamageModifier = 80;

		public override object Create(ActorInitializer init) { return new HealthScaledDamageMultiplier(init.Self, this); }
	}

	public class HealthScaledDamageMultiplier : ConditionalTrait<HealthScaledDamageMultiplierInfo>, IDamageModifier
	{
		readonly int modifier;

		public HealthScaledDamageMultiplier(Actor self, HealthScaledDamageMultiplierInfo info)
			: base(info)
		{
			modifier = 100;
			var healthInfo = self.Info.TraitInfoOrDefault<HealthInfo>();
			if (healthInfo == null)
				return;

			var health = healthInfo.HP;
			if (info.MinHealth == info.MaxHealth)
			{
				modifier = health == info.MinHealth ? info.MinDamageModifier : 100;
			}
			else
			{
				var healthRange = info.MaxHealth - info.MinHealth;
				var modifierRange = info.MaxDamageModifier - info.MinDamageModifier;
				var healthOffset = health - info.MinHealth;

				if (health <= info.MinHealth)
					modifier = info.MinDamageModifier;
				else if (health >= info.MaxHealth)
					modifier = info.MaxDamageModifier;
				else
					modifier = info.MinDamageModifier + modifierRange * healthOffset / healthRange;
			}
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return IsTraitDisabled ? 100 : modifier;
		}
	}
}
