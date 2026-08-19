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

		readonly WPos[] offsets;
		readonly float3[] screenPoints;
		readonly int zOffset;
		readonly WDist width;
		readonly Color color;
		readonly Color glowColor;
		readonly float glowScale;
		readonly float glowIntensity;

		public DistortionHaloRenderable(WPos[] offsets, float3[] screenPoints, int zOffset, WDist width, Color color,
			Color glowColor, float glowScale, float glowIntensity)
		{
			this.offsets = offsets;
			this.screenPoints = screenPoints;
			this.zOffset = zOffset;
			this.width = width;
			this.color = color;
			this.glowColor = glowColor;
			this.glowScale = glowScale;
			this.glowIntensity = glowIntensity;
		}

		public WPos Pos => offsets[0];
		public PaletteReference Palette => null;
		public int ZOffset => zOffset;
		public bool IsDecoration => true;

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return new DistortionHaloRenderable(offsets, screenPoints, newOffset, width, color, glowColor, glowScale, glowIntensity); }
		public IRenderable OffsetBy(in WVec offset)
		{
			var translatedOffsets = new WPos[offsets.Length];
			for (var i = 0; i < offsets.Length; i++)
				translatedOffsets[i] = offsets[i] + offset;

			return new DistortionHaloRenderable(translatedOffsets, new float3[offsets.Length + 1], zOffset, width, color, glowColor, glowScale, glowIntensity);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];
			for (var i = 0; i < offsets.Length; i++)
				screenPoints[i] = wr.Viewport.WorldToViewPx(wr.ScreenPosition(offsets[i]));

			screenPoints[offsets.Length] = screenPoints[0];
			Game.Renderer.RgbaColorRenderer.DrawLine(screenPoints, screenWidth, color, false);

			if (!Game.Settings.Graphics.WeaponPostfx || glowScale <= 0f)
				return;

			var glowRenderer = wr.World.WorldActor.TraitOrDefault<GlowRenderer>();
			var glowSegments = Math.Min(MaximumGlowSegmentsPerLine, offsets.Length);
			for (var i = 0; i < glowSegments; i++)
			{
				var startIndex = i * offsets.Length / glowSegments;
				var endIndex = (i + 1) * offsets.Length / glowSegments % offsets.Length;
				glowRenderer?.RegisterGlow(offsets[startIndex], offsets[endIndex], glowColor, glowScale, intensity: glowIntensity);
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}