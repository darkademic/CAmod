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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	// The base type prevents Harvester.OnDockCompleted from queuing an unrestricted search.
	public class FindAndDeliverResourcesAtNode : FindAndDeliverResources
	{
		sealed class Node
		{
			public Target NodeTarget;
			public Target LastKnownTarget;
			public uint ActorId;

			public Node(in Target target)
			{
				NodeTarget = target;
				LastKnownTarget = Target.FromTargetPositions(target);
				ActorId = target.Type == TargetType.Actor ? target.Actor.ActorID : target.FrozenActor.ID;
			}
		}

		readonly HarvestsNode harvestsNode;
		readonly Harvester harvester;
		readonly Mobile mobile;
		readonly ResourceClaimLayer claimLayer;
		readonly DockClientManager dockClient;
		readonly MoveCooldownHelper moveCooldownHelper;
		readonly List<Node> nodes = new();
		Node currentNode;
		CPos? harvestCell;
		bool waitingForResources;
		CPos? teleportDestination;
		ChronoResourceDeliveryInfo teleportInfo;
		Actor teleportRefinery;

		public FindAndDeliverResourcesAtNode(Actor self, in Target target, HarvestsNode harvestsNode)
			: base(self)
		{
			this.harvestsNode = harvestsNode;
			harvester = self.Trait<Harvester>();
			mobile = self.Trait<Mobile>();
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
			dockClient = self.Trait<DockClientManager>();
			moveCooldownHelper = new MoveCooldownHelper(self.World, mobile) { RetryIfDestinationBlocked = true };
			ChildHasPriority = false;
			currentNode = new Node(target);
			nodes.Add(currentNode);
		}

		public Target CurrentNode => currentNode?.LastKnownTarget ?? Target.Invalid;

		public IEnumerable<Target> NodeTargets(Actor self)
		{
			foreach (var node in nodes)
			{
				var target = node.NodeTarget.Recalculate(self.Owner, out var hidden);
				if (hidden)
					yield return node.LastKnownTarget;
				else if (harvestsNode.CanTarget(self, target))
					yield return target;
			}
		}

		public void AddNode(Actor self, in Target target)
		{
			var actorId = target.Type == TargetType.Actor ? target.Actor.ActorID : target.FrozenActor.ID;
			if (nodes.Any(node => node.ActorId == actorId))
				return;

			nodes.Add(new Node(target));
			if (waitingForResources)
				ChildActivity?.Cancel(self);
		}

		public bool TryTeleportToRefinery(Actor self, CPos destination, ChronoResourceDeliveryInfo info, Actor refinery)
		{
			if (IsCanceling || teleportDestination.HasValue || ChildActivity is not MoveToDock)
				return false;

			teleportDestination = destination;
			teleportInfo = info;
			teleportRefinery = refinery;
			ChildActivity.Cancel(self);
			return true;
		}

		public override bool Tick(Actor self)
		{
			if (!IsCanceling)
			{
				if (harvester.IsTraitDisabled || harvestsNode.IsTraitDisabled)
					Cancel(self, true);
				else
				{
					UpdateNodes(self);
					if (nodes.Count == 0)
						Cancel(self, true);
					else if (!nodes.Contains(currentNode))
					{
						currentNode = NodesByDistance(self).First();
						if (harvestCell.HasValue)
							ChildActivity?.Cancel(self);
					}
					else if (harvestCell.HasValue && !IsInRange(self, harvestCell.Value, NodeCell(self, currentNode)))
						ChildActivity?.Cancel(self);
				}
			}

			if (!TickChild(self))
				return false;

			harvestCell = null;
			waitingForResources = false;
			if (IsCanceling)
				return true;

			if (teleportDestination.HasValue)
			{
				var destination = teleportDestination.Value;
				var refinery = teleportRefinery;
				var info = teleportInfo;
				ClearTeleport();

				if (!refinery.IsDead && refinery.IsInWorld && self.Trait<IPositionable>().CanEnterCell(destination))
				{
					QueueChild(new ChronoResourceTeleport(destination, info, self.Location, refinery, false));
					QueueChild(new MoveToDock(self, refinery, dockLineColor: dockClient.DockLineColor));
				}
				else
					QueueChild(new MoveToDock(self, dockLineColor: dockClient.DockLineColor));

				return false;
			}

			var cooldownResult = moveCooldownHelper.Tick(false);
			if (cooldownResult != null)
				return cooldownResult.Value;

			if (harvester.IsFull)
			{
				if (dockClient.ReservedHost == null)
					QueueChild(new MoveToDock(self, dockLineColor: dockClient.DockLineColor));

				return false;
			}

			var cell = ClosestHarvestableCell(self, NodeCell(self, currentNode));
			if (!cell.HasValue)
			{
				foreach (var node in NodesByDistance(self))
				{
					if (node == currentNode)
						continue;

					cell = ClosestHarvestableCell(self, NodeCell(self, node));
					if (!cell.HasValue)
						continue;

					currentNode = node;
					break;
				}
			}

			if (cell.HasValue)
			{
				harvestCell = cell;
				moveCooldownHelper.NotifyMoveQueued();
				QueueChild(new HarvestResource(self, cell.Value));
				return false;
			}

			if (!harvester.IsEmpty)
			{
				if (dockClient.ReservedHost == null)
					QueueChild(new MoveToDock(self, dockLineColor: dockClient.DockLineColor));

				return false;
			}

			waitingForResources = true;
			var lastDock = dockClient.LastReservedHost;
			if (lastDock != null)
			{
				var dockCell = self.World.Map.CellContaining(lastDock.DockPosition);
				if (self.Location == dockCell)
				{
					var unblockCell = dockCell + harvester.Info.UnblockCell;
					var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);
					moveCooldownHelper.NotifyMoveQueued();
					QueueChild(mobile.MoveTo(moveTo, 1));
				}
			}

			QueueChild(new Wait(harvester.Info.WaitDuration));
			return false;
		}

		void UpdateNodes(Actor self)
		{
			for (var i = nodes.Count - 1; i >= 0; i--)
			{
				var node = nodes[i];
				node.NodeTarget = node.NodeTarget.Recalculate(self.Owner, out var hidden);
				if (hidden)
					continue;

				if (!harvestsNode.CanTarget(self, node.NodeTarget))
				{
					nodes.RemoveAt(i);
					continue;
				}

				node.LastKnownTarget = Target.FromTargetPositions(node.NodeTarget);
				node.ActorId = node.NodeTarget.Type == TargetType.Actor ? node.NodeTarget.Actor.ActorID : node.NodeTarget.FrozenActor.ID;
			}
		}

		IEnumerable<Node> NodesByDistance(Actor self)
		{
			return nodes.OrderBy(node => (node.LastKnownTarget.CenterPosition - self.CenterPosition).HorizontalLengthSquared);
		}

		static CPos NodeCell(Actor self, Node node)
		{
			return self.World.Map.CellContaining(node.LastKnownTarget.CenterPosition);
		}

		bool IsInRange(Actor self, CPos cell, CPos center)
		{
			var map = self.World.Map;
			return (map.CenterOfCell(cell) - map.CenterOfCell(center)).HorizontalLengthSquared <= harvestsNode.Info.MaxRange.LengthSquared;
		}

		CPos? ClosestHarvestableCell(Actor self, CPos center)
		{
			if (IsInRange(self, self.Location, center) && harvester.CanHarvestCell(self.Location)
				&& claimLayer.CanClaimCell(self, self.Location))
				return self.Location;

			var searchRadius = (int)(((long)harvestsNode.Info.MaxRange.Length + 1023) / 1024);
			var cells = self.World.Map.FindTilesInCircle(center, searchRadius)
				.Where(cell => IsInRange(self, cell, center) && harvester.CanHarvestCell(cell) && claimLayer.CanClaimCell(self, cell))
				.ToArray();

			if (cells.Length == 0)
				return null;

			var path = mobile.PathFinder.FindPathToTargetCells(self, self.Location, cells, BlockedByActor.Stationary);
			return path.Count > 0 ? path[0] : null;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			ClearTeleport();
			base.Cancel(self, keepQueue);
		}

		void ClearTeleport()
		{
			teleportDestination = null;
			teleportInfo = null;
			teleportRefinery = null;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var node in ChildActivity.TargetLineNodes(self))
					yield return node;

			if (CurrentNode.Type != TargetType.Invalid)
				yield return new TargetLineNode(CurrentNode, harvester.Info.HarvestLineColor);
		}
	}
}
