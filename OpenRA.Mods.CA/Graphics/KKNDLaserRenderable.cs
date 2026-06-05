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
	public struct KKNDLaserRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos[] offsets;
		readonly int zOffset;
		readonly WDist width;
		readonly Color color;
		readonly Color glowColor;
		readonly float glowScale;
		readonly float glowIntensity;

		public KKNDLaserRenderable(WPos[] offsets, int zOffset, WDist width, Color color, Color glowColor, float glowScale, float glowIntensity)
		{
			this.offsets = offsets;
			this.zOffset = zOffset;
			this.width = width;
			this.color = color;
			this.glowColor = glowColor;
			this.glowScale = glowScale;
			this.glowIntensity = glowIntensity;
		}

		public WPos Pos { get { return new WPos(offsets[0].X, offsets[0].Y, 0); } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return new KKNDLaserRenderable(offsets, newOffset, width, color, glowColor, glowScale, glowIntensity); }
		public IRenderable OffsetBy(in WVec offset)
		{
			var offsetBy = offset; return new KKNDLaserRenderable(offsets.Select(o => o + offsetBy).
			ToArray(), zOffset, width, color, glowColor, glowScale, glowIntensity);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];

			if (Game.Settings.Graphics.WeaponPostfx && glowScale > 0f && offsets.Length != 0)
				wr.World.WorldActor.TraitOrDefault<GlowRenderer>()
					?.RegisterGlow(offsets.FirstOrDefault(), offsets.LastOrDefault(), glowColor, glowScale, intensity: glowIntensity);

			Game.Renderer.WorldRgbaColorRenderer.DrawLine(offsets.Select(offset => wr.Screen3DPosition(offset)), screenWidth, color, false);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
