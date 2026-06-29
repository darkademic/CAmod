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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.CA.Effects
{
	public sealed class DistortionHaloAnimation
	{
		const int MinimumPointsPerLine = 8;

		readonly struct HaloLine
		{
			public HaloLine(Color color, WAngle baseAngle, WPos[] positions, WVec[] distortions)
			{
				Color = color;
				BaseAngle = baseAngle;
				Positions = positions;
				Distortions = distortions;
			}

			public Color Color { get; }
			public WAngle BaseAngle { get; }
			public WPos[] Positions { get; }
			public WVec[] Distortions { get; }
		}

		readonly MersenneTwister random;
		readonly HaloLine[] lines;
		readonly WDist radius;
		readonly int distortion;
		readonly int distortionAnimation;
		readonly int maxDistortion;
		readonly WAngle rotationAnimation;
		readonly int pointsPerLine;

		int ticks;
		WAngle rotation;
		WPos center;

		public DistortionHaloAnimation(MersenneTwister random, WPos center, WDist radius, int lineCount,
			Color[] colors, WAngle lineArc, int segmentsPerLine, int distortion, int distortionAnimation,
			int maxDistortion,
			WAngle rotationAnimation)
		{
			this.random = random;
			this.center = center;
			this.radius = radius;
			this.distortion = Math.Max(0, distortion);
			this.distortionAnimation = Math.Max(0, distortionAnimation);
			this.maxDistortion = Math.Max(0, maxDistortion);
			this.rotationAnimation = rotationAnimation;
			pointsPerLine = Math.Max(MinimumPointsPerLine, segmentsPerLine);

			var normalizedColors = colors != null && colors.Length > 0
				? colors
				: new[] { Color.FromArgb(255, 255, 255, 255) };

			var actualLineCount = Math.Max(1, lineCount);
			lines = new HaloLine[actualLineCount];
			for (var i = 0; i < actualLineCount; i++)
			{
				var color = normalizedColors[i % normalizedColors.Length];
				var angle = WAngle.FromDegrees(i * 360 / actualLineCount);
				lines[i] = new HaloLine(color, angle, new WPos[pointsPerLine], new WVec[pointsPerLine]);
			}

			UpdatePositions(true);
		}

		public void Tick(WPos center)
		{
			this.center = center;
			rotation += rotationAnimation;
			UpdatePositions(false);
			ticks++;
		}

		public IEnumerable<IRenderable> Render(int zOffset, WDist width, Color glowColor, float glowScale, float glowIntensity)
		{
			foreach (var line in lines)
				yield return new DistortionHaloRenderable(line.Positions, zOffset, width, line.Color, glowColor, glowScale, glowIntensity);
		}

		void UpdatePositions(bool initializing)
		{
			var shouldDistort = (ticks == 0 && distortion > 0) || (ticks > 0 && distortionAnimation > 0);

			for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
			{
				var positions = lines[lineIndex].Positions;
				var distortions = lines[lineIndex].Distortions;
				for (var pointIndex = 0; pointIndex < pointsPerLine; pointIndex++)
				{
					var angle = AngleFor(lines[lineIndex].BaseAngle, pointIndex);
					var basePos = BasePosition(angle);

					if (shouldDistort)
					{
						var magnitudeLimit = ticks > 0 ? distortionAnimation : distortion;
						if (magnitudeLimit > 0)
							distortions[pointIndex] = ClampDistortion(distortions[pointIndex] + RandomDistortion(magnitudeLimit));
					}

					positions[pointIndex] = basePos + ProjectDistortion(angle, distortions[pointIndex]);
				}
			}
		}

		WAngle AngleFor(WAngle baseAngle, int pointIndex)
		{
			if (pointsPerLine <= 0)
				return baseAngle + rotation;

			var startAngle = baseAngle + rotation;
			var step = 1024 / pointsPerLine;
			return startAngle + new WAngle(step * pointIndex);
		}

		WPos BasePosition(WAngle angle)
		{
			var radial = new WVec(angle.Cos(), angle.Sin(), 0);
			return center + radius.Length * radial / 1024;
		}

		WVec RandomDistortion(int magnitudeLimit)
		{
			var magnitude = random.Next(magnitudeLimit + 1);
			if (magnitude == 0)
				return WVec.Zero;

			var localAngle = WAngle.FromDegrees(random.Next(360));
			return new WVec(
				magnitude * localAngle.Cos() / 1024,
				magnitude * localAngle.Sin() / 1024,
				0);
		}

		WVec ProjectDistortion(WAngle angle, WVec localDistortion)
		{
			var radial = new WVec(angle.Cos(), angle.Sin(), 0);
			var tangent = new WVec(-angle.Sin(), angle.Cos(), 0);

			return localDistortion.X * tangent / 1024
				+ localDistortion.Y * radial / 1024;
		}

		WVec ClampDistortion(WVec localDistortion)
		{
			if (maxDistortion <= 0 || localDistortion.Length <= maxDistortion)
				return localDistortion;

			return maxDistortion * localDistortion / localDistortion.Length;
		}
	}

	public sealed class DistortionHaloEffect : IEffect
	{
		readonly DistortionHaloAnimation halo;
		readonly int duration;
		readonly int zOffset;
		readonly WDist width;
		readonly Color glowColor;
		readonly float glowScale;
		readonly float glowIntensity;
		readonly WPos center;

		int ticks;

		public DistortionHaloEffect(World world, WPos center, WDist radius, int lineCount, WDist width,
			Color[] colors, int duration, int zOffset, int distortion, int distortionAnimation,
			int maxDistortion,
			WAngle lineArc, int segmentsPerLine, WAngle rotationAnimation,
			Color glowColor, float glowScale, float glowIntensity)
		{
			this.center = center;
			this.width = width;
			this.duration = Math.Max(1, duration);
			this.zOffset = zOffset;
			this.glowColor = glowColor;
			this.glowScale = glowScale;
			this.glowIntensity = glowIntensity;

			halo = new DistortionHaloAnimation(world.SharedRandom, center, radius, lineCount, colors,
				lineArc, segmentsPerLine, distortion, distortionAnimation, maxDistortion, rotationAnimation);
		}

		public void Tick(World world)
		{
			if (++ticks >= duration)
			{
				world.AddFrameEndTask(w => w.Remove(this));
				return;
			}

			halo.Tick(center);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return halo.Render(zOffset, width, glowColor, glowScale, glowIntensity);
		}
	}
}