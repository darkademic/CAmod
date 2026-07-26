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
	[Desc("Intercepts regular attack orders against out-of-range actor targets and ignores them while enabled.")]
	public class IgnoreOutOfRangeAttackOrdersInfo : ConditionalTraitInfo
	{
		[Desc("Order priority used to override the default attack targeter.")]
		public readonly int Priority = 7;

		[CursorReference]
		[Desc("Cursor to display when hovering over a valid target that is outside of range.")]
		public readonly string OutsideRangeCursor = "default";

		[Desc("If true, will ignore out of range attack orders against terrain.")]
		public readonly bool IgnoreOutOfRangeTerrainAttacks = true;

		[Desc("If true, will ignore out of range attack orders that are forced by the player.")]
		public readonly bool IgnoreForceAttacks = true;

		public override object Create(ActorInitializer init) { return new IgnoreOutOfRangeAttackOrders(init, this); }
	}

	public class IgnoreOutOfRangeAttackOrders : ConditionalTrait<IgnoreOutOfRangeAttackOrdersInfo>, IIssueOrder, IResolveOrder, INotifyCreated
	{
		const string IgnoreOrderId = "IgnoreOutOfRangeAttackOrders";

		AttackBase[] attackBases = System.Array.Empty<AttackBase>();

		public IgnoreOutOfRangeAttackOrders(ActorInitializer init, IgnoreOutOfRangeAttackOrdersInfo info)
			: base(info) { }

		void INotifyCreated.Created(Actor self)
		{
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new IgnoreOutOfRangeAttackOrderTargeter(this);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order is IgnoreOutOfRangeAttackOrderTargeter)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == IgnoreOrderId)
			{
				// Intentionally absorb out-of-range regular attack orders while enabled.
			}
		}

		bool ShouldIgnoreOrder(Actor self, in Target target, bool forceAttack, out string cursor)
		{
			cursor = null;

			if (target.Type != TargetType.Actor && target.Type != TargetType.FrozenActor && !Info.IgnoreOutOfRangeTerrainAttacks)
				return false;

			Armament bestOutOfRangeArmament = null;
			AttackBase bestOutOfRangeAttackBase = null;

			foreach (var attackBase in attackBases)
			{
				var armament = attackBase.ChooseArmamentsForTarget(target, forceAttack)
					.OrderBy(x => x.IsTraitPaused)
					.ThenByDescending(x => x.MaxRange())
					.FirstOrDefault();

				if (armament == null)
					continue;

				if (target.IsInRange(self.CenterPosition, armament.MaxRange()))
					return false;

				if (bestOutOfRangeArmament == null || armament.MaxRange() > bestOutOfRangeArmament.MaxRange())
				{
					bestOutOfRangeArmament = armament;
					bestOutOfRangeAttackBase = attackBase;
				}
			}

			if (bestOutOfRangeArmament == null)
				return false;

			cursor = Info.OutsideRangeCursor ?? bestOutOfRangeAttackBase.Info.OutsideRangeCursor ?? bestOutOfRangeArmament.Info.OutsideRangeCursor;
			return true;
		}

		sealed class IgnoreOutOfRangeAttackOrderTargeter : IOrderTargeter
		{
			readonly IgnoreOutOfRangeAttackOrders parent;

			public IgnoreOutOfRangeAttackOrderTargeter(IgnoreOutOfRangeAttackOrders parent)
			{
				this.parent = parent;
			}

			public string OrderID => IgnoreOrderId;
			public int OrderPriority => parent.Info.Priority;
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
				var forceAttack = modifiers.HasModifier(TargetModifiers.ForceAttack);

				if (parent.IsTraitDisabled)
					return false;

				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				if (!parent.Info.IgnoreForceAttacks && forceAttack)
					return false;

				return parent.ShouldIgnoreOrder(self, target, forceAttack, out cursor);
			}

			public bool IsQueued { get; private set; }
		}
	}
}
