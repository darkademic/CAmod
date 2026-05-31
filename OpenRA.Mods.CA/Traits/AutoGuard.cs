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
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach to support unit so that when ordered as part of a group with combat units it will guard those units.")]
	class AutoGuardInfo : ConditionalTraitInfo
	{
		[Desc("Will only guard units with these target types.")]
		public readonly BitSet<TargetableType> ValidTargets = new BitSet<TargetableType>("Ground", "Water");

		[Desc("Will only guard units with these target types.")]
		public readonly HashSet<string> ValidOrders = new HashSet<string>() { "AttackMove", "AssaultMove", "Attack", "ForceAttack", "KeepDistance" };

		[Desc("Maximum number of guard orders to chain together.")]
		public readonly int MaxTargets = 8;

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.OrangeRed;

		[Desc("Maximum range that guarding actors will maintain.")]
		public readonly WDist Range = WDist.FromCells(2);

		[Desc("Maximum range to scan for guard targets.")]
		public readonly WDist MaxDistance = WDist.FromCells(6);

		[Desc("If guard targets are this much further from the target, don't guard them.")]
		public readonly WDist MaxDistanceIfFurtherFromTarget = WDist.FromCells(3);

		public override object Create(ActorInitializer init) { return new AutoGuard(init, this); }
	}

	class AutoGuard : ConditionalTrait<AutoGuardInfo>, IResolveOrder, INotifyCreated
	{
		IMove move;
		AutoTarget autoTarget;

		public AutoGuard(ActorInitializer init, AutoGuardInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			move = self.Trait<IMove>();
			autoTarget = self.TraitOrDefault<AutoTarget>();
			base.Created(self);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.Target.Type == TargetType.Invalid)
				return;

			if (order.Queued)
				return;

			if (!Info.ValidOrders.Contains(order.OrderString))
				return;

			if (self.Owner.IsBot)
				return;

			if (order.Target.Type == TargetType.Actor && (order.Target.Actor.Disposed || order.Target.Actor.Owner == self.Owner || !order.Target.Actor.IsInWorld || order.Target.Actor.IsDead))
				return;

			if (autoTarget != null && autoTarget.Stance < UnitStance.Defend)
				return;

			var orderTargetPosition = order.Target.CenterPosition;
			var selfDistanceToTarget = (self.CenterPosition - orderTargetPosition).HorizontalLength;

			var guardActors = self.World.FindActorsInCircle(self.CenterPosition, Info.MaxDistance)
				.Where(a => a.Owner == self.Owner
					&& !a.Disposed
					&& !a.IsDead
					&& a.IsInWorld
					&& a != self
					&& IsValidGuardTarget(self, a, orderTargetPosition, selfDistanceToTarget))
				.OrderBy(a => (a.CenterPosition - order.Target.CenterPosition).LengthSquared)
				.ThenBy(a => a.ActorID)
				.Take(Info.MaxTargets)
				.ToArray();

			if (guardActors.Length == 0)
				return;

			self.World.AddFrameEndTask(_ =>
			{
				if (self.Disposed || self.IsDead || !self.IsInWorld)
					return;

				for (var i = 0; i < guardActors.Length; i++)
					QueueGuardTarget(self, Target.FromActor(guardActors[i]), i > 0);

				self.ShowTargetLines();
			});
		}

		void QueueGuardTarget(Actor self, in Target target, bool queued)
		{
			var targetCopy = target;

			if (target.Type != TargetType.Actor)
				return;

			var range = target.Actor.Info.TraitInfo<GuardableInfo>().Range;
			self.QueueActivity(queued, new AttackMoveActivity(self, () => move.MoveFollow(self, targetCopy, WDist.Zero, range, targetLineColor: Info.TargetLineColor)));
		}

		bool IsValidGuardTarget(Actor self, Actor targetActor, WPos orderTargetPosition, int selfDistanceToTarget)
		{
			if (!Info.ValidTargets.Overlaps(targetActor.GetEnabledTargetTypes()))
				return false;

			if (!targetActor.Info.HasTraitInfo<AttackBaseInfo>() || !targetActor.Info.HasTraitInfo<GuardableInfo>())
				return false;

			var AutoGuard = targetActor.TraitsImplementing<AutoGuard>();
			if (AutoGuard.Any(t => !t.IsTraitDisabled))
				return false;

			if ((targetActor.CenterPosition - self.CenterPosition).HorizontalLengthSquared > Info.MaxDistance.LengthSquared)
				return false;

			var targetDistanceToOrder = (targetActor.CenterPosition - orderTargetPosition).HorizontalLength;
			if (targetDistanceToOrder >= selfDistanceToTarget + Info.MaxDistanceIfFurtherFromTarget.Length)
				return false;

			return true;
		}
	}
}
