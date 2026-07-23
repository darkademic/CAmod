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
	public enum SpawnedActorIdentifierType
	{
		Circle,
		Rectangle,
		SelectionBox,
	}

	[Desc("Draws a marker around actors tracked by SpawnActorAbility while selected.")]
	public class WithSpawnedActorIdentifierInfo : TraitInfo
	{
		[Desc("Shape used to identify each tracked spawned actor.")]
		public readonly SpawnedActorIdentifierType Type = SpawnedActorIdentifierType.Circle;

		[Desc("Radius of the marker when Type is Circle.")]
		public readonly WDist Radius = new WDist(512);

		[Desc("Color of the marker.")]
		public readonly Color Color = Color.FromArgb(255, Color.Red);

		[Desc("Line width used by Circle and Rectangle.")]
		public readonly int Width = 1;

		[Desc("Additional padding applied to decoration bounds when Type is Rectangle or SelectionBox.")]
		public readonly int2 BoundsMargin = int2.Zero;

		[Desc("If set, the owner player's color will be used instead of Color.")]
		public readonly bool UsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] used when UsePlayerColor is enabled.")]
		public readonly int PlayerColorAlpha = 255;

		public override object Create(ActorInitializer init) { return new WithSpawnedActorIdentifier(init.Self, this); }
	}

	public class WithSpawnedActorIdentifier : IRenderAnnotationsWhenSelected, INotifyCreated
	{
		readonly WithSpawnedActorIdentifierInfo info;
		SpawnActorAbility spawnActorAbility;

		public WithSpawnedActorIdentifier(Actor self, WithSpawnedActorIdentifierInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			spawnActorAbility = self.TraitOrDefault<SpawnActorAbility>();
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (spawnActorAbility == null || spawnActorAbility.Info.ConcurrentLimit <= 0)
				yield break;

			var color = info.UsePlayerColor ? Color.FromArgb(info.PlayerColorAlpha, self.OwnerColor()) : info.Color;

			foreach (var spawned in spawnActorAbility.SpawnedActors)
			{
				if (spawned == null || spawned.Disposed || spawned.IsDead || !spawned.IsInWorld)
					continue;

				if (info.Type == SpawnedActorIdentifierType.Circle)
				{
					yield return new CircleAnnotationRenderable(
						spawned.CenterPosition,
						info.Radius,
						info.Width,
						color);
					continue;
				}

				var interactable = spawned.TraitOrDefault<Interactable>();
				if (interactable == null)
					continue;

				var bounds = interactable.DecorationBounds(spawned, wr);
				var markerBounds = new Rectangle(
					bounds.X - info.BoundsMargin.X,
					bounds.Y - info.BoundsMargin.Y,
					bounds.Width + 2 * info.BoundsMargin.X,
					bounds.Height + 2 * info.BoundsMargin.Y);

				if (info.Type == SpawnedActorIdentifierType.SelectionBox)
				{
					yield return new SelectionBoxAnnotationRenderable(spawned, markerBounds, color);
					continue;
				}

				yield return new RectangleAnnotationRenderable(spawned, markerBounds, info.Width, color);
			}
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}

	sealed class RectangleAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly Rectangle bounds;
		readonly int width;
		readonly Color color;

		public RectangleAnnotationRenderable(Actor actor, Rectangle bounds, int width, Color color)
			: this(actor.CenterPosition, bounds, width, color) { }

		RectangleAnnotationRenderable(WPos pos, Rectangle bounds, int width, Color color)
		{
			Pos = pos;
			this.bounds = bounds;
			this.width = width;
			this.color = color;
		}

		public WPos Pos { get; }
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return new RectangleAnnotationRenderable(Pos + vec, bounds, width, color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var tl = wr.Viewport.WorldToViewPx(new float2(bounds.Left, bounds.Top)).ToFloat2();
			var br = wr.Viewport.WorldToViewPx(new float2(bounds.Right, bounds.Bottom)).ToFloat2();
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);

			var cr = Game.Renderer.RgbaColorRenderer;
			cr.DrawLine(new float3[] { tl, tr, br, bl, tl }, width, color, true);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}