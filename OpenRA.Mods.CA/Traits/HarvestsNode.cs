#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers.
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows a harvester to be ordered to persistently harvest resources near target nodes.",
		"Consecutive queued node orders form a pool; other orders wait until it is canceled or all nodes are lost.")]
	public class HarvestsNodeInfo : ConditionalTraitInfo, Requires<HarvesterInfo>, Requires<MobileInfo>,
		Requires<DockClientManagerInfo>
	{
		[Desc("Targetable types that can be used as resource nodes.")]
		public readonly BitSet<TargetableType> TargetTypes = default;

		[Desc("Maximum horizontal distance from the node's cell center to a harvestable resource cell center.",
			"Movement and deliveries may travel outside this radius.")]
		public readonly WDist MaxRange = WDist.FromCells(6);

		[Desc("Color of the node range circles, including alpha.",
			"Defaults to the harvester's harvest line color with 50% opacity.")]
		public readonly Color? RangeCircleColor = null;

		public override void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			base.RulesetLoaded(rules, info);

			if (MaxRange < WDist.Zero)
				throw new YamlException("HarvestsNode.MaxRange must be non-negative.");

			if (!TargetTypes.Any())
				throw new YamlException("HarvestsNode.TargetTypes must not be empty.");
		}

		public override object Create(ActorInitializer init) { return new HarvestsNode(init.Self, this); }
	}

	public class HarvestsNode : ConditionalTrait<HarvestsNodeInfo>, IIssueOrder, IResolveOrder, IOrderVoice,
		IRenderAnnotationsWhenSelected
	{
		readonly Harvester harvester;
		readonly HarvesterInfo harvesterInfo;

		public HarvestsNode(Actor self, HarvestsNodeInfo info)
			: base(info)
		{
			if (info.MaxRange.Length > (long)self.World.Map.Grid.MaximumTileSearchRange * 1024)
				throw new YamlException($"HarvestsNode.MaxRange exceeds MaximumTileSearchRange ({self.World.Map.Grid.MaximumTileSearchRange} cells).");

			harvester = self.Trait<Harvester>();
			harvesterInfo = self.Info.TraitInfo<HarvesterInfo>();
		}

		public bool CanTarget(Actor self, in Target target)
		{
			if (!target.IsValidFor(self))
				return false;

			return target.Type switch
			{
				TargetType.Actor => Info.TargetTypes.Overlaps(target.Actor.GetEnabledTargetTypes()),
				TargetType.FrozenActor => Info.TargetTypes.Overlaps(target.FrozenActor.TargetTypes),
				_ => false
			};
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled && !harvester.IsTraitDisabled)
					yield return new HarvestNodeOrderTargeter(this, harvesterInfo.HarvestCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == "HarvestNode" ? new Order(order.OrderID, self, target, queued) : null;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "HarvestNode" ? harvesterInfo.HarvestVoice : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "HarvestNode" || IsTraitDisabled || harvester.IsTraitDisabled)
				return;

			var target = order.Target.Recalculate(self.Owner, out var hidden);
			if (hidden || !CanTarget(self, target))
				return;

			if (order.Queued)
			{
				var last = self.CurrentActivity;
				while (last?.NextActivity != null)
					last = last.NextActivity;

				if (last is FindAndDeliverResourcesAtNode nodeActivity
					&& !nodeActivity.IsCanceling && nodeActivity.State != ActivityState.Done)
				{
					nodeActivity.AddNode(self, target);
					self.ShowTargetLines();
					return;
				}
			}

			self.QueueActivity(order.Queued, new FindAndDeliverResourcesAtNode(self, target, this));
			self.ShowTargetLines();
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (self.CurrentActivity is not FindAndDeliverResourcesAtNode activity || activity.IsCanceling
				|| !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var centers = new HashSet<CPos>();
			for (Activity queued = activity; queued != null; queued = queued.NextActivity)
			{
				if (queued is not FindAndDeliverResourcesAtNode nodeActivity || nodeActivity.IsCanceling)
					continue;

				foreach (var node in nodeActivity.NodeTargets(self))
				{
					var center = self.World.Map.CellContaining(node.CenterPosition);
					if (!centers.Add(center))
						continue;

					yield return new RangeCircleAnnotationRenderable(
						self.World.Map.CenterOfCell(center), Info.MaxRange, 0,
						Info.RangeCircleColor ?? Color.FromArgb(128, harvesterInfo.HarvestLineColor),
						1, Color.FromArgb(96, Color.Black), 3);
				}
			}
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		sealed class HarvestNodeOrderTargeter : UnitOrderTargeter
		{
			readonly HarvestsNode harvestsNode;

			public HarvestNodeOrderTargeter(HarvestsNode harvestsNode, string cursor)
				: base("HarvestNode", 10, cursor, true, true)
			{
				this.harvestsNode = harvestsNode;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return !modifiers.HasModifier(TargetModifiers.ForceMove)
					&& harvestsNode.CanTarget(self, Target.FromActor(target));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return !modifiers.HasModifier(TargetModifiers.ForceMove)
					&& harvestsNode.CanTarget(self, Target.FromFrozenActor(target));
			}
		}
	}
}
