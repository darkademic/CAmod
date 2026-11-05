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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Circle overlay that oscillates between its start and end radii.")]
	class WithOscillatingCircleInfo : ConditionalTraitInfo
	{
		[Desc("Circle line width.")]
		public readonly int Width = 1;

		[Desc("Color of the circle outline.")]
		public readonly Color Color = Color.FromArgb(80, Color.Red);

		[Desc("Optional fill color of the circle, including its alpha.")]
		public readonly Color? FillColor = null;

		[Desc("Initial radius of the circle.")]
		public readonly WDist StartRadius = WDist.Zero;

		[Desc("Radius the circle reaches before returning to its start radius.")]
		public readonly WDist EndRadius = new WDist(1024);

		[Desc("Number of ticks to travel between the start and end radii in either direction.")]
		public readonly int Duration = 25;

		[Desc("Player relationships which will be able to see the circle.",
			"Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("When to show the circle. Valid values are `Always`, and `WhenSelected`.")]
		public readonly RadiatingCircleVisibility Visible = RadiatingCircleVisibility.WhenSelected;

		public override object Create(ActorInitializer init) { return new WithOscillatingCircle(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (StartRadius < WDist.Zero || EndRadius < WDist.Zero)
				throw new YamlException($"WithOscillatingCircle on actor '{ai.Name}' requires non-negative StartRadius and EndRadius.");

			if (Duration <= 0)
				throw new YamlException($"WithOscillatingCircle on actor '{ai.Name}' requires a positive Duration.");
		}
	}

	class WithOscillatingCircle : ConditionalTrait<WithOscillatingCircleInfo>, ITick, IRenderAnnotationsWhenSelected, IRenderAnnotations
	{
		readonly Actor self;
		int elapsed;
		bool movingToEnd = true;

		public WithOscillatingCircle(Actor self, WithOscillatingCircleInfo info)
			: base(info)
		{
			this.self = self;
		}

		bool VisibleToPlayer
		{
			get
			{
				if (IsTraitDisabled)
					return false;

				var p = self.World.RenderPlayer;
				return p == null || Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(p)) || (p.Spectating && !p.NonCombatant);
			}
		}

		IEnumerable<IRenderable> RenderCircle(WorldRenderer wr, RadiatingCircleVisibility visibility)
		{
			if (Info.Visible != visibility || !VisibleToPlayer)
				yield break;

			var radius = Info.StartRadius.Length + ((long)Info.EndRadius.Length - Info.StartRadius.Length) * elapsed / Info.Duration;
			var circleRadius = new WDist((int)radius);
			var center = self.CenterPosition;
			if (Info.FillColor.HasValue)
			{
				var offset = new WVec(circleRadius.Length, circleRadius.Length, 0);
				var tl = wr.Viewport.WorldToViewPx(wr.ScreenPosition(center - offset));
				var br = wr.Viewport.WorldToViewPx(wr.ScreenPosition(center + offset));
				if (br.X > tl.X && br.Y > tl.Y)
					yield return new CircleAnnotationRenderable(center, circleRadius, Info.Width, Info.FillColor.Value, true);
			}

			yield return new CircleAnnotationRenderable(center, circleRadius, Info.Width, Info.Color, false);
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderCircle(wr, RadiatingCircleVisibility.WhenSelected);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderCircle(wr, RadiatingCircleVisibility.Always);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			elapsed += movingToEnd ? 1 : -1;
			if (elapsed == Info.Duration)
				movingToEnd = false;
			else if (elapsed == 0)
				movingToEnd = true;
		}

		protected override void TraitEnabled(Actor self)
		{
			elapsed = 0;
			movingToEnd = true;
		}
	}
}
