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
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders a distorted halo of animated arc segments around the actor.")]
	class WithDistortionHaloInfo : ConditionalTraitInfo
	{
		[Desc("Radius of the halo.")]
		public readonly WDist Radius = new WDist(1024);

		[Desc("Number of full distorted halo lines around the ring.")]
		public readonly int LineCount = 8;

		[Desc("Width of each halo line.")]
		public readonly WDist Width = new WDist(12);

		[Desc("Colors of the halo lines. Alpha is taken from the color values.")]
		public readonly Color[] Colors =
		{
			Color.FromArgb(192, 80, 80, 255),
			Color.FromArgb(192, 255, 255, 255)
		};

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Initial distortion offset applied to each line.")]
		public readonly int Distortion = 0;

		[Desc("Additional distortion added per tick.")]
		public readonly int DistortionAnimation = 0;

		[Desc("Maximum distance any point may drift from its base ring position. 0 disables the clamp.")]
		public readonly int MaxDistortion = 0;

		[Desc("Unused for full-ring halo lines. Retained for backwards compatibility.")]
		public readonly WAngle LineArc = new WAngle(64);

		[Desc("Number of points used to approximate each halo line around the full circumference.")]
		public readonly int SegmentsPerLine = 32;

		[Desc("Rotation added to the halo each tick.")]
		public readonly WAngle RotationAnimation = WAngle.Zero;

		[Desc("Color of the screen-space glow halo drawn along each line.",
			"Only visible when the \"Weapon Glow Effects\" setting is enabled.")]
		public readonly Color GlowColor = Color.FromArgb(255, 51, 255);

		[Desc("Scale multiplier for the glow halo's radius (also scales intensity).",
			"Set to 0 to disable the glow for this halo.")]
		public readonly float GlowScale = 0.4f;

		[Desc("Brightness-only multiplier for the glow halo, independent of GlowScale (does not grow the radius).")]
		public readonly float GlowIntensity = 1.65f;

		[Desc("Player relationships which will be able to see the halo.",
			"Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("When to show the halo. Valid values are `Always`, and `WhenSelected`")]
		public readonly RadiatingCircleVisibility Visible = RadiatingCircleVisibility.WhenSelected;

		public override object Create(ActorInitializer init) { return new WithDistortionHalo(init.Self, this); }
	}

	class WithDistortionHalo : ConditionalTrait<WithDistortionHaloInfo>, ITick, IRenderAnnotationsWhenSelected, IRenderAnnotations
	{
		public new readonly WithDistortionHaloInfo Info;
		readonly Actor self;
		DistortionHaloAnimation halo;

		public WithDistortionHalo(Actor self, WithDistortionHaloInfo info)
			: base(info)
		{
			this.self = self;
			Info = info;
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

		IEnumerable<IRenderable> RenderHalo(RadiatingCircleVisibility visibility)
		{
			if (Info.Visible != visibility || !VisibleToPlayer || halo == null)
				yield break;

			foreach (var renderable in halo.Render(Info.ZOffset, Info.Width, Info.GlowColor, Info.GlowScale, Info.GlowIntensity))
				yield return renderable;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderHalo(RadiatingCircleVisibility.WhenSelected);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderHalo(RadiatingCircleVisibility.Always);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		void ITick.Tick(Actor self)
		{
			if (!VisibleToPlayer || halo == null)
				return;

			halo.Tick(self.CenterPosition);
		}

		protected override void TraitEnabled(Actor self)
		{
			halo = new DistortionHaloAnimation(self.World.SharedRandom, self.CenterPosition, Info.Radius,
				Info.LineCount, Info.Colors, Info.LineArc, Info.SegmentsPerLine, Info.Distortion,
				Info.DistortionAnimation, Info.MaxDistortion, Info.RotationAnimation);
		}

		protected override void TraitDisabled(Actor self)
		{
			halo = null;
		}
	}
}