#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach this to actors which should regenerate or lose shield points over time.")]
	sealed class ChangesShieldStrengthInfo : ConditionalTraitInfo, Requires<ShieldedInfo>
	{
		[Desc("Absolute amount of shield points added in each step.",
			"Use negative values to apply damage.")]
		public readonly int Step = 5;

		[Desc("Relative percentages of shield added in each step.",
			"Use negative values to apply damage.",
			"When both values are defined, their summary will be applied.")]
		public readonly int PercentageStep = 0;

		[Desc("Time in ticks to wait between each shield modification.")]
		public readonly int Delay = 5;

		[Desc("Regen if current shield is below this percentage of full shields.")]
		public readonly int StartIfBelow = 50;

		[Desc("Time in ticks to wait after taking damage.")]
		public readonly int DamageCooldown = 0;

		public override object Create(ActorInitializer init) { return new ChangesShieldStrength(init.Self, this); }
	}

	sealed class ChangesShieldStrength : ConditionalTrait<ChangesShieldStrengthInfo>, ITick, INotifyDamage, ISync
	{
		readonly Shielded shielded;

		[Sync]
		int ticks;

		[Sync]
		int damageTicks;

		public ChangesShieldStrength(Actor self, ChangesShieldStrengthInfo info)
			: base(info)
		{
			shielded = self.Trait<Shielded>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			if (shielded.StrengthPercent >= Info.StartIfBelow)
				return;

			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.Delay;

				if (Info.PercentageStep > 0)
					shielded.RechargeByPercent(Info.PercentageStep);

				if (Info.Step > 0)
					shielded.RechargeByAmount(Info.Step);
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
