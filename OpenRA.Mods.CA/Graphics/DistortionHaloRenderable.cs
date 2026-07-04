#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Graphics
{
	public readonly struct DistortionHaloRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos[] offsets;
		readonly int zOffset;
		readonly WDist width;
		readonly Color color;
		readonly Color glowColor;
		readonly float glowScale;
		readonly float glowIntensity;

		public DistortionHaloRenderable(WPos[] offsets, int zOffset, WDist width, Color color,
			Color glowColor, float glowScale, float glowIntensity)
		{
			this.offsets = offsets;
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
		public IRenderable WithZOffset(int newOffset) { return new DistortionHaloRenderable(offsets, newOffset, width, color, glowColor, glowScale, glowIntensity); }
		public IRenderable OffsetBy(in WVec offset)
		{
			var offsetBy = offset;
			return new DistortionHaloRenderable(offsets.Select(o => o + offsetBy).ToArray(), zOffset, width, color, glowColor, glowScale, glowIntensity);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];
			var cr = Game.Renderer.RgbaColorRenderer;
			var glowRenderer = Game.Settings.Graphics.WeaponPostfx && glowScale > 0f
				? wr.World.WorldActor.TraitOrDefault<GlowRenderer>()
				: null;
			var previousWorld = offsets[offsets.Length - 1];
			var previousScreen = wr.Viewport.WorldToViewPx(wr.ScreenPosition(previousWorld));

			for (var i = 0; i < offsets.Length; i++)
			{
				var currentWorld = offsets[i];
				var currentScreen = wr.Viewport.WorldToViewPx(wr.ScreenPosition(currentWorld));
				cr.DrawLine(previousScreen, currentScreen, screenWidth, color);
				glowRenderer?.RegisterGlow(previousWorld, currentWorld, glowColor, glowScale, intensity: glowIntensity);
				previousWorld = currentWorld;
				previousScreen = currentScreen;
			}
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}