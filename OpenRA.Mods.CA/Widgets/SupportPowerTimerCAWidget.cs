#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class SupportPowerTimerCAWidget : Widget
	{
		[FluentReference("support-power")]
		const string NameFormat = "support-power-timer-name";

		[FluentReference]
		const string EnemyHeading = "support-power-timer-enemy";

		[FluentReference]
		const string AlliedHeading = "support-power-timer-allied";

		const int HeaderHeight = 26;
		const int CategorySpacing = 10;

		public readonly string Font = "Bold";
		public readonly string HeadingFont = "Bold";
		public readonly TextAlign Align = TextAlign.Left;
		public readonly string[] Superweapons = { "Lightning Storm", "Atom Bomb", "Ion Cannon", "Chemical Missile", "Rift" };

		readonly SpriteFont font;
		SpriteFont headingFont;
		readonly World world;
		readonly IEnumerable<SupportPowerInstance> powers;
		readonly CachedTransform<string, Sprite> getIcon = new(name => ChromeProvider.GetImage("scrollpanel-decorations", name));
		readonly string buttonCursor = ChromeMetrics.Get<string>("ButtonCursor");
		readonly Color bgDark, bgLight;
		List<(string Name, string Time, Color NameColor, Color TimeColor)> enemyTexts = new();
		List<(string Name, string Time, Color NameColor, Color TimeColor)> alliedTexts = new();
		bool enemyExpanded = true;
		bool alliedExpanded = true;
		HeaderGroup? pressedHeader;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerCAWidget(World world)
		{
			this.world = world;
			powers = world.ActorsWithTrait<SupportPowerManager>()
				.Where(p => !p.Actor.IsDead && !p.Actor.Owner.NonCombatant)
				.SelectMany(s => s.Trait.Powers.Values)
				.Where(p => p.Instances.Count > 0 && p.Info.DisplayTimerRelationships != PlayerRelationship.None && !p.Disabled);

			bgDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
			bgLight = ChromeMetrics.Get<Color>("TextContrastColorDark");
			font = Game.Renderer.Fonts[Font];
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);
			headingFont = Game.Renderer.Fonts[HeadingFont];
		}

		public override void Tick()
		{
			enemyTexts.Clear();
			alliedTexts.Clear();

			var viewer = world.RenderPlayer ?? world.LocalPlayer;
			var orderedPowers = powers
				.GroupBy(p => p.Instances[0].Self.Owner)
				.OrderBy(group => group.Key == viewer ? 0 : 1)
				.SelectMany(group => group.OrderBy(p => p.Info.SupportPowerPaletteOrder));

			foreach (var power in orderedPowers)
			{
				var self = power.Instances[0].Self;
				var owner = self.Owner;
				var relationship = owner.RelationshipWith(viewer);

				if (viewer != null && !power.Info.DisplayTimerRelationships.HasRelationship(relationship))
					continue;

				var time = WidgetUtils.FormatTime(power.RemainingTicks, false, self.World.Timestep);
				var name = FluentProvider.GetMessage(NameFormat, "support-power", power.Name);

				if (Superweapons.Contains(power.Info.Name))
					name = $"★ {name}";

				var ownerColor = self.OwnerColor();
				var timeColor = !power.Ready || Game.LocalTick % 50 < 25 ? ownerColor : Lighten(ownerColor);
				if (relationship == PlayerRelationship.Enemy)
					enemyTexts.Add((name, time, ownerColor, timeColor));
				else
					alliedTexts.Add((name, time, ownerColor, timeColor));
			}

			UpdateHeight();
		}

		public override void Draw()
		{
			if (!IsVisible())
				return;

			var y = 0;
			DrawGroup(alliedTexts, AlliedHeading, alliedExpanded, ref y);
			DrawGroup(enemyTexts, EnemyHeading, enemyExpanded, ref y);
		}

		void DrawGroup(List<(string Name, string Time, Color NameColor, Color TimeColor)> texts, string headingKey, bool expanded, ref int y)
		{
			if (texts.Count == 0)
				return;

			var heading = FluentProvider.GetMessage(headingKey);
			var icon = getIcon.Update(expanded ? "down" : "right");
			WidgetUtils.DrawSprite(icon, new int2(RenderOrigin.X, RenderOrigin.Y + y + 7));

			var headingSize = headingFont.Measure(heading);
			var headingLocation = new int2(RenderOrigin.X + 20, RenderOrigin.Y + y + (HeaderHeight - headingSize.Y) / 2);
			headingFont.DrawTextWithShadow(heading, headingLocation, Color.White, bgDark, bgLight, 1);

			if (!expanded)
			{
				y += HeaderHeight + CategorySpacing;
				return;
			}

			var timerY = y + HeaderHeight;

			foreach (var t in texts)
			{
				var nameSize = font.Measure(t.Name);
				var timeSize = font.Measure(t.Time);
				var separatorWidth = font.Measure(" ").X;
				var rowWidth = nameSize.X + separatorWidth + timeSize.X;
				var x = RenderOrigin.X;

				if (Align == TextAlign.Center)
					x += (Bounds.Width - 24 - rowWidth) / 2;

				if (Align == TextAlign.Right)
					x += Bounds.Width - 24 - rowWidth;

				var location = new float2(x, RenderOrigin.Y + timerY);
				font.DrawTextWithShadow(t.Name, location, t.NameColor, bgDark, bgLight, 1);
				font.DrawTextWithShadow(t.Time, location + new int2(nameSize.X + separatorWidth, 0), t.TimeColor, bgDark, bgLight, 1);
				var textSize = font.Measure(t.Name + " " + t.Time);
				timerY += textSize.Y + 5;
			}

			y += GroupHeight(texts, expanded);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down)
			{
				var enemyHeader = GetHeaderBounds(true);
				var alliedHeader = GetHeaderBounds(false);
				if ((!enemyHeader.HasValue || !enemyHeader.Value.Contains(mi.Location)) &&
					(!alliedHeader.HasValue || !alliedHeader.Value.Contains(mi.Location)))
					return false;

				if (!TakeMouseFocus(mi))
					return false;

				pressedHeader = enemyHeader?.Contains(mi.Location) == true ? HeaderGroup.Enemy : HeaderGroup.Allied;
				return true;
			}

			if (mi.Event != MouseInputEvent.Up || !HasMouseFocus)
				return false;

			var clickedEnemy = pressedHeader == HeaderGroup.Enemy && GetHeaderBounds(true)?.Contains(mi.Location) == true;
			var clickedAllied = pressedHeader == HeaderGroup.Allied && GetHeaderBounds(false)?.Contains(mi.Location) == true;
			pressedHeader = null;
			if (clickedEnemy)
				enemyExpanded = !enemyExpanded;
			else if (clickedAllied)
				alliedExpanded = !alliedExpanded;

			UpdateHeight();
			YieldMouseFocus(mi);
			return true;
		}

		public override string GetCursor(int2 pos)
		{
			return GetHeaderBounds(true)?.Contains(pos) == true || GetHeaderBounds(false)?.Contains(pos) == true
				? buttonCursor
				: base.GetCursor(pos);
		}

		Rectangle? GetHeaderBounds(bool enemy)
		{
			var y = 0;
			if (alliedTexts.Count > 0)
			{
				if (!enemy)
					return new Rectangle(RenderOrigin.X, RenderOrigin.Y + y, Bounds.Width, HeaderHeight);

				y += GroupHeight(alliedTexts, alliedExpanded);
			}

			if (enemy && enemyTexts.Count > 0)
				return new Rectangle(RenderOrigin.X, RenderOrigin.Y + y, Bounds.Width, HeaderHeight);

			return null;
		}

		void UpdateHeight()
		{
			var height = 0;
			if (enemyTexts.Count > 0)
				height += GroupHeight(enemyTexts, enemyExpanded);

			if (alliedTexts.Count > 0)
				height += GroupHeight(alliedTexts, alliedExpanded);

			Bounds.Height = height;
		}

		int GroupHeight(List<(string Name, string Time, Color NameColor, Color TimeColor)> texts, bool expanded)
		{
			var height = HeaderHeight + CategorySpacing;
			if (expanded)
				height += TimerHeight(texts);

			return height;
		}

		int TimerHeight(List<(string Name, string Time, Color NameColor, Color TimeColor)> texts)
		{
			var height = 0;
			foreach (var text in texts)
				height += font.Measure(text.Name + " " + text.Time).Y + 5;

			return height;
		}

		static Color Lighten(Color color)
		{
			return Color.FromArgb(color.A,
				color.R + (byte.MaxValue - color.R) / 2,
				color.G + (byte.MaxValue - color.G) / 2,
				color.B + (byte.MaxValue - color.B) / 2);
		}

		enum HeaderGroup { Enemy, Allied }

	}
}
