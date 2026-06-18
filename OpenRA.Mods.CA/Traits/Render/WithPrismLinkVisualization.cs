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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders selection boxes on Prism Towers within range.")]
	sealed class WithPrismLinkVisualizationInfo : ConditionalTraitInfo, IPlaceBuildingDecorationInfo
	{
		[Desc("Color of the links when selecting an existing node.")]
		public readonly Color SelectedColor = Color.FromArgb(128, Color.Cyan);

		[Desc("Color of the links when placing a new node.")]
		public readonly Color PlacementColor = Color.FromArgb(128, Color.Cyan);

		[Desc("Color of the range circle.")]
		public readonly Color RangeCircleColor = Color.FromArgb(128, Color.Cyan);

		[Desc("Range of the circle")]
		public readonly WDist Range = WDist.Zero;

		public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (!EnabledByDefault)
				yield break;

			var supportInfo = ai.TraitInfoOrDefault<AttackPrismSupportedInfo>();
			if (supportInfo == null)
				yield break;

			var roots = w.FindActorsInCircle(centerPosition, Range)
				.Where(a => a.Info.HasTraitInfo<WithPrismLinkVisualizationInfo>()
					&& a.Info.HasTraitInfo<AttackPrismSupportedInfo>()
					&& a.Trait<WithPrismLinkVisualization>().IsVisible(supportInfo.CanSupportAllies));
			var placementMaxHops = Math.Max(0, supportInfo.MaxHops - 1);

			yield return new RangeCircleAnnotationRenderable(
				centerPosition,
				Range,
				0,
				RangeCircleColor,
				1,
				Color.Transparent,
				0);

			foreach (var renderable in PrismLinkVisualizationRenderer.RenderNetwork(w, wr, roots, centerPosition, placementMaxHops, PlacementColor, null, supportInfo.CanSupportAllies))
				yield return renderable;
		}

		public override object Create(ActorInitializer init) { return new WithPrismLinkVisualization(init.Self, this); }
	}

	sealed class WithPrismLinkVisualization : ConditionalTrait<WithPrismLinkVisualizationInfo>, IRenderAnnotationsWhenSelected
	{
		readonly Actor self;
		readonly AttackPrismSupportedInfo supportInfo;

		public WithPrismLinkVisualization(Actor self, WithPrismLinkVisualizationInfo info)
			: base(info)
		{
			this.self = self;
			supportInfo = self.Info.TraitInfoOrDefault<AttackPrismSupportedInfo>();
		}

		public bool IsVisible(bool allowAllies)
		{
			if (IsTraitDisabled)
				return false;

			var p = self.World.RenderPlayer;
			return p == null || self.Owner == p || (allowAllies && self.Owner.IsAlliedWith(p));
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (supportInfo == null)
				yield break;

			if (!IsVisible(supportInfo.CanSupportAllies))
				yield break;

			if (self.World.Selection.Actors.Count != 1 || !self.World.Selection.Contains(self))
				yield break;

			var root = self;
			foreach (var renderable in PrismLinkVisualizationRenderer.RenderNetwork(self.World, wr, new[] { root }, null, supportInfo.MaxHops, Info.SelectedColor, ShouldRenderSelectedEdge, supportInfo.CanSupportAllies, false))
				yield return renderable;
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		bool ShouldRenderSelectedEdge(Actor source, Actor target)
		{
			if (!self.World.Selection.Contains(target))
				return true;

			return source.ActorID < target.ActorID;
		}
	}

	static class PrismLinkVisualizationRenderer
	{
		static ulong EdgeKey(Actor a, Actor b)
		{
			var minId = Math.Min(a.ActorID, b.ActorID);
			var maxId = Math.Max(a.ActorID, b.ActorID);
			return ((ulong)minId << 32) | maxId;
		}

		public static IEnumerable<IRenderable> RenderNetwork(
			World world,
			WorldRenderer wr,
			IEnumerable<Actor> roots,
			WPos? source,
			int maxHops,
			Color color,
			Func<Actor, Actor, bool> shouldRenderEdge,
			bool allowAllies,
			bool selfMustBeIdle = true)
		{
			var candidateActors = world.ActorsHavingTrait<AttackPrismSupported>()
				.Where(a => a.Info.HasTraitInfo<WithPrismLinkVisualizationInfo>())
				.Where(a => IsOwnedOrAllied(a, allowAllies))
				.ToArray();

			var visitedActors = new HashSet<Actor>();
			var renderedEdges = new HashSet<ulong>();
			var queue = new Queue<(Actor Actor, int Hops)>();

			foreach (var root in roots)
			{
				if (source.HasValue)
					yield return new LineAnnotationRenderable(root.CenterPosition, source.Value, 1, color);

				queue.Enqueue((root, 0));
			}

			while (queue.Count > 0)
			{
				var (node, hops) = queue.Dequeue();
				if (hops >= maxHops)
					continue;

				foreach (var adjacent in GetValidNeighborSupporters(node, candidateActors, selfMustBeIdle))
				{
					if (shouldRenderEdge != null && !shouldRenderEdge(node, adjacent))
						continue;

					var edgeKey = EdgeKey(node, adjacent);
					if (!renderedEdges.Add(edgeKey))
						continue;

					yield return new LineAnnotationRenderable(node.CenterPosition, adjacent.CenterPosition, 1, color);
					queue.Enqueue((adjacent, hops + 1));
				}
			}
		}

		static IEnumerable<Actor> GetValidNeighborSupporters(Actor self, IEnumerable<Actor> traitedActors, bool selfMustBeIdle)
		{
			foreach (var candidate in traitedActors)
			{
				if (candidate == self)
					continue;

				var candidateAttack = candidate.Trait<AttackPrismSupported>();
				if (!candidateAttack.MaySupport(candidate, self, selfMustBeIdle))
					continue;

				if (candidateAttack.CheckSupportRange(candidate, self))
					yield return candidate;
			}
		}

		static bool IsOwnedOrAllied(Actor actor, bool allowAllies)
		{
			var p = actor.World.RenderPlayer;
			return p == null || actor.Owner == p || (allowAllies && actor.Owner.IsAlliedWith(p));
		}
	}
}
