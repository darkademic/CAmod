#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Creates a temporary distorted halo of animated arc segments at the impact position. Purely visual, no damage.")]
	public class CreateDistortionHaloWarhead : Warhead
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

		[Desc("Lifetime of the effect in ticks.")]
		public readonly int Duration = 5;

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

		public override bool IsValidAgainst(Actor victim, Actor firedBy) => true;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var pos = args.ImpactPosition != WPos.Zero ? args.ImpactPosition : target.CenterPosition;
			args.SourceActor.World.AddFrameEndTask(w => w.Add(new DistortionHaloEffect(w, pos, Radius, LineCount,
				Width, Colors, Duration, ZOffset, Distortion, DistortionAnimation, MaxDistortion, LineArc,
				SegmentsPerLine, RotationAnimation, GlowColor, GlowScale, GlowIntensity)));
		}
	}
}