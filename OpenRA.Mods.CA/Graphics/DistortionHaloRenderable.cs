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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Graphics
{
	public readonly struct DistortionHaloRenderable : IRenderable, IFinalizedRenderable
	{
		const int MaximumGlowSegmentsPerLine = 8;

		readonly WPos[][] lineOffsets;
		readonly float3[][] lineScreenPoints;
		readonly int zOffset;
		readonly WDist width;
		readonly Color[] lineColors;
		readonly Color glowColor;
		readonly float glowScale;
		readonly float glowIntensity;

		public DistortionHaloRenderable(WPos[][] lineOffsets, float3[][] lineScreenPoints, int zOffset, WDist width, Color[] lineColors,
			Color glowColor, float glowScale, float glowIntensity)
		{
			this.lineOffsets = lineOffsets;
			this.lineScreenPoints = lineScreenPoints;
			this.zOffset = zOffset;
			this.width = width;
			this.lineColors = lineColors;
			this.glowColor = glowColor;
			this.glowScale = glowScale;
			this.glowIntensity = glowIntensity;
		}

		public WPos Pos => lineOffsets[0][0];
		public PaletteReference Palette => null;
		public int ZOffset => zOffset;
		public bool IsDecoration => true;

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return new DistortionHaloRenderable(lineOffsets, lineScreenPoints, newOffset, width, lineColors, glowColor, glowScale, glowIntensity); }
		public IRenderable OffsetBy(in WVec offset)
		{
			var translatedLineOffsets = new WPos[lineOffsets.Length][];
			var translatedLineScreenPoints = new float3[lineOffsets.Length][];
			for (var lineIndex = 0; lineIndex < lineOffsets.Length; lineIndex++)
			{
				var offsets = lineOffsets[lineIndex];
				translatedLineOffsets[lineIndex] = new WPos[offsets.Length];
				translatedLineScreenPoints[lineIndex] = new float3[offsets.Length + 1];
				for (var pointIndex = 0; pointIndex < offsets.Length; pointIndex++)
					translatedLineOffsets[lineIndex][pointIndex] = offsets[pointIndex] + offset;
			}

			return new DistortionHaloRenderable(translatedLineOffsets, translatedLineScreenPoints, zOffset, width, lineColors, glowColor, glowScale, glowIntensity);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];
			for (var lineIndex = 0; lineIndex < lineOffsets.Length; lineIndex++)
			{
				var offsets = lineOffsets[lineIndex];
				var screenPoints = lineScreenPoints[lineIndex];
				for (var pointIndex = 0; pointIndex < offsets.Length; pointIndex++)
					screenPoints[pointIndex] = wr.Viewport.WorldToViewPx(wr.ScreenPosition(offsets[pointIndex]));

				screenPoints[offsets.Length] = screenPoints[0];
				Game.Renderer.RgbaColorRenderer.DrawLine(screenPoints, screenWidth, lineColors[lineIndex], false);
			}

			if (!Game.Settings.Graphics.WeaponPostfx || glowScale <= 0f)
				return;

			var glowRenderer = wr.World.WorldActor.TraitOrDefault<GlowRenderer>();
			for (var lineIndex = 0; lineIndex < lineOffsets.Length; lineIndex++)
			{
				var offsets = lineOffsets[lineIndex];
				var glowSegments = Math.Min(MaximumGlowSegmentsPerLine, offsets.Length);
				for (var segmentIndex = 0; segmentIndex < glowSegments; segmentIndex++)
				{
					var startIndex = segmentIndex * offsets.Length / glowSegments;
					var endIndex = (segmentIndex + 1) * offsets.Length / glowSegments % offsets.Length;
					glowRenderer?.RegisterGlow(offsets[startIndex], offsets[endIndex], glowColor, glowScale, intensity: glowIntensity);
				}
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}