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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Flags]
	public enum ExtendableTimedConditionExtensionType
	{
		None = 0,
		DamageDealt = 1
	}

	[Desc("Gives a condition to the actor for a limited time.")]
	public class GrantExtendableTimedConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 50;

		[Desc("If true, the condition will only start to drain after the actor attacks.")]
		public readonly bool DischargeOnAttack = false;

		[Desc("If true, condition will last for full duration once enabled, even if trait is subsequently disabled/paused.")]
		public readonly bool ForceFullDuration = false;

		[Desc("List actions which extend the duration.")]
		public readonly ExtendableTimedConditionExtensionType ExtensionTypes = ExtendableTimedConditionExtensionType.None;

		[Desc("Ticks to extend the condition duration when an extension event occurs.")]
		public readonly int ExtensionTicks = 0;

		[Desc("Maximum remaining duration, in ticks, after an extension.")]
		public readonly int MaxExtensionTicks = 0;

		[Desc("If true, extending events will take effect after the condition has expired,",
			"otherwise the trait needs to be re-enabled to refresh the initial duration first.")]
		public readonly bool CanExtendAfterExpiration = false;

		[Desc("Minimum damage required to trigger an extension.")]
		public int MinDamage = 1;

		public override object Create(ActorInitializer init) { return new GrantExtendableTimedCondition(this); }
	}

	public class GrantExtendableTimedCondition : PausableConditionalTrait<GrantExtendableTimedConditionInfo>, ITick, ISync, INotifyCreated, INotifyAppliedDamage, INotifyAttack
	{
		readonly GrantExtendableTimedConditionInfo info;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;
		bool hasExtended;
		bool attacked;

		[Sync]
		public int Ticks { get; private set; }

		public GrantExtendableTimedCondition(GrantExtendableTimedConditionInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.Duration;
			hasExtended = false;
		}

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void GrantCondition(Actor self, string condition)
		{
			if (token == Actor.InvalidConditionToken)
			{
				Ticks = info.Duration;
				token = self.GrantCondition(condition);
			}
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
			{
				token = self.RevokeCondition(token);
				UpdateWatchers(0);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (!Info.ForceFullDuration || Ticks <= 0)
			{
				if (IsTraitDisabled && token != Actor.InvalidConditionToken)
					RevokeCondition(self);

				if (IsTraitPaused || IsTraitDisabled)
					return;
			}

			if (token == Actor.InvalidConditionToken)
				return;

			UpdateWatchers(Ticks);

			if (Info.DischargeOnAttack && !attacked)
				return;

			if (--Ticks < 1)
				RevokeCondition(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			hasExtended = false;
			attacked = false;
			GrantCondition(self, info.Condition);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (token == Actor.InvalidConditionToken || (!Info.ForceFullDuration && (IsTraitDisabled || IsTraitPaused)))
				return;

			attacked = true;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (info.ExtensionTicks <= 0 || info.MaxExtensionTicks <= 0)
				return;

			if (!info.ExtensionTypes.HasFlag(ExtendableTimedConditionExtensionType.DamageDealt))
				return;

			if (token == Actor.InvalidConditionToken)
			{
				if (!info.CanExtendAfterExpiration || Ticks > 0)
					return;
			}
			else if (!info.ForceFullDuration && (IsTraitDisabled || IsTraitPaused))
				return;

			if (self.Owner.RelationshipWith(damaged.Owner) != PlayerRelationship.Enemy)
				return;

			if (e.Damage.Value < info.MinDamage)
				return;

			var extendedTicks = Math.Min(info.MaxExtensionTicks, (long)Ticks + info.ExtensionTicks);
			if (extendedTicks <= Ticks)
				return;

			Ticks = (int)extendedTicks;
			hasExtended = true;
			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
		}

		void UpdateWatchers(int remainingTicks)
		{
			var maxTicks = hasExtended ? info.MaxExtensionTicks : Math.Max(info.Duration, info.MaxExtensionTicks);
			foreach (var w in watchers)
				w.Update(maxTicks, remainingTicks);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }
	}
}
