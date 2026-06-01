#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enhanced version of WarheadDebugOverlay that supports custom shapes. Attach this to the world actor.")]
	public class WarheadDebugOverlayCAInfo : TraitInfo
	{
		public readonly int DisplayDuration = 25;

		public override object Create(ActorInitializer init) { return new WarheadDebugOverlayCA(this); }
	}

	public class WarheadDebugOverlayCA : IRenderAnnotations
	{
		sealed class WHImpact
		{
			public readonly WPos CenterPosition;
			public readonly WDist[] Range;
			public readonly Color Color;
			public int Time;

			public WDist OuterRange => Range[^1];

			public WHImpact(WPos pos, WDist[] range, int time, Color color)
			{
				CenterPosition = pos;
				Range = range;
				Color = color;
				Time = time;
			}
		}

		readonly WarheadDebugOverlayCAInfo info;
		readonly List<WHImpact> impacts = new();
		readonly List<WHPoylineImpact> polylineImpacts = new();

		public WarheadDebugOverlayCA(WarheadDebugOverlayCAInfo info)
		{
			this.info = info;
		}

		public void AddImpact(WPos pos, WDist[] range, Color color)
		{
			impacts.Add(new WHImpact(pos, range, info.DisplayDuration, color));
		}

		sealed class WHPoylineImpact
		{
			public readonly WPos[] Points;
			public readonly int Width;
			public readonly Color Color;
			public int Time;

			public WHPoylineImpact(WPos[] points, int width, int time, Color color)
			{
				Points = points;
				Width = width;
				Time = time;
				Color = color;
			}
		}

		public void AddPolygonOutline(WPos[] points, Color color, int width = 1)
		{
			if (points == null || points.Length < 2)
				return;
			polylineImpacts.Add(new WHPoylineImpact(points, width, info.DisplayDuration, color));
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			// Render standard circular impacts
			foreach (var i in impacts)
			{
				var alpha = 255.0f * i.Time / info.DisplayDuration;
				var rangeStep = alpha / i.Range.Length;

				yield return new CircleAnnotationRenderable(i.CenterPosition, i.OuterRange, 1, Color.FromArgb((int)alpha, i.Color));

				foreach (var r in i.Range)
				{
					yield return new CircleAnnotationRenderable(i.CenterPosition, r, 1, Color.FromArgb((int)alpha, i.Color), true);
					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					i.Time--;
			}

			impacts.RemoveAll(i => i.Time == 0);

			// Render polygon outlines
			foreach (var p in polylineImpacts)
			{
				var alpha = 255.0f * p.Time / info.DisplayDuration;
				var col = Color.FromArgb((int)alpha, p.Color);

				for (var i = 0; i < p.Points.Length; i++)
				{
					var a = p.Points[i];
					var b = p.Points[(i + 1) % p.Points.Length];
					yield return new LineAnnotationRenderable(a, b, p.Width, col);
				}

				if (!wr.World.Paused)
					p.Time--;
			}

			polylineImpacts.RemoveAll(p => p.Time == 0);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
