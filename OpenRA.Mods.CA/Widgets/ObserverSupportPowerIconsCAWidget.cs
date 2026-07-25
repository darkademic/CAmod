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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ObserverSupportPowerIconsCAWidget : Widget
	{
		public readonly string TooltipTemplate = "SUPPORT_POWER_TOOLTIP";
		public readonly string TooltipContainer;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly Dictionary<string, Animation> clocks;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;

		public Func<SupportPowersWidget.SupportPowerIcon> GetTooltipIcon;
		public SupportPowersWidget.SupportPowerIcon TooltipIcon { get; private set; }

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 1;
		public int MinWidth = 240;

		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";
		public Func<Player> GetPlayer;

		readonly List<SupportPowersWidget.SupportPowerIcon> supportPowerIcons = new();
		readonly List<Rectangle> supportPowerIconBounds = new();
		readonly float2 iconSize;
		Animation icon;
		int lastIconIdx;
		int currentTooltipToken;

		[ObjectCreator.UseCtor]
		public ObserverSupportPowerIconsCAWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<string, Animation>();
			iconSize = new float2(IconWidth, IconHeight);

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ObserverSupportPowerIconsCAWidget(ObserverSupportPowerIconsCAWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			icon = other.icon;
			world = other.world;
			worldRenderer = other.worldRenderer;
			clocks = other.clocks;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpacing = other.IconSpacing;
			iconSize = new float2(IconWidth, IconHeight);
			MinWidth = other.MinWidth;

			ClockAnimation = other.ClockAnimation;
			ClockSequence = other.ClockSequence;
			ClockPalette = other.ClockPalette;

			TooltipIcon = other.TooltipIcon;
			GetTooltipIcon = () => TooltipIcon;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void Draw()
		{
			supportPowerIcons.Clear();
			supportPowerIconBounds.Clear();

			var player = GetPlayer();
			if (player == null)
				return;

			var powers = player.PlayerActor.Trait<SupportPowerManager>().Powers
				.Where(x => !x.Value.Disabled)
				.OrderBy(p => p.Value.Info.SupportPowerPaletteOrder)
				.Select((a, i) => new { a, i })
				.ToList();

			foreach (var power in powers)
			{
				if (!clocks.ContainsKey(power.a.Key))
					clocks.Add(power.a.Key, new Animation(world, ClockAnimation));
			}

			Game.Renderer.EnableAntialiasingFilter();

			foreach (var power in powers)
			{
				var item = power.a.Value;
				if (item == null || item.Info == null || item.Info.Icon == null)
					continue;

				icon = new Animation(worldRenderer.World, item.Info.IconImage);
				icon.Play(item.Info.Icon);
				var location = new float2(RenderBounds.Location) + new float2(power.i * (IconWidth + IconSpacing), 0);

				supportPowerIcons.Add(new SupportPowersWidget.SupportPowerIcon { Power = item, Pos = location });
				supportPowerIconBounds.Add(new Rectangle((int)location.X, (int)location.Y, (int)iconSize.X, (int)iconSize.Y));

				WidgetUtils.DrawSpriteCentered(icon.Image, worldRenderer.Palette(item.Info.IconPalette), location + 0.5f * iconSize, 0.5f);

				var clock = clocks[power.a.Key];
				clock.PlayFetchIndex(ClockSequence,
					() => item.TotalTicks == 0 ? 0 : ((item.TotalTicks - item.RemainingTicks)
						* (clock.CurrentSequence.Length - 1) / item.TotalTicks));
				clock.Tick();
				WidgetUtils.DrawSpriteCentered(clock.Image, worldRenderer.Palette(ClockPalette), location + 0.5f * iconSize, 0.5f);
			}

			var newWidth = Math.Max(powers.Count * (IconWidth + IconSpacing), MinWidth);
			if (newWidth != Bounds.Width)
			{
				var wasInBounds = EventBounds.Contains(Viewport.LastMousePos);
				Bounds.Width = newWidth;
				var isInBounds = EventBounds.Contains(Viewport.LastMousePos);

				if (wasInBounds != isInBounds)
					Game.RunAfterTick(Ui.ResetTooltips);
			}

			Game.Renderer.DisableAntialiasingFilter();

			var tiny = Game.Renderer.Fonts["Tiny"];
			foreach (var supportPowerIcon in supportPowerIcons)
			{
				var text = GetOverlayForItem(supportPowerIcon.Power, world.Timestep);
				tiny.DrawTextWithContrast(text,
					supportPowerIcon.Pos + new float2(16, 12) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);
			}

			var parentWidth = Bounds.X + Bounds.Width;
			Parent.Bounds.Width = parentWidth;

			var gradient = Parent.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			var offset = gradient.Bounds.X - Bounds.X;
			var gradientWidth = Math.Max(MinWidth - offset, powers.Count * (IconWidth + IconSpacing));

			gradient.Bounds.Width = gradientWidth;
			var widestChildWidth = Parent.Parent.Children.Max(x => x.Bounds.Width);

			Parent.Parent.Bounds.Width = Math.Max(25 + widestChildWidth, Bounds.Left + MinWidth);
		}

		static string GetOverlayForItem(SupportPowerInstance item, int timestep)
		{
			if (item.Disabled)
				return "ON HOLD";

			if (item.Ready)
				return "READY";

			return WidgetUtils.FormatTime(item.RemainingTicks, timestep);
		}

		public override Widget Clone()
		{
			return new ObserverSupportPowerIconsCAWidget(this);
		}

		public override void Tick()
		{
			if (TooltipContainer == null)
				return;

			if (Ui.MouseOverWidget != this)
			{
				if (TooltipIcon != null)
				{
					tooltipContainer.Value.RemoveTooltip(currentTooltipToken);
					lastIconIdx = 0;
					TooltipIcon = null;
				}

				return;
			}

			if (TooltipIcon != null &&
				lastIconIdx < supportPowerIconBounds.Count &&
				supportPowerIcons[lastIconIdx].Power == TooltipIcon.Power &&
				supportPowerIconBounds[lastIconIdx].Contains(Viewport.LastMousePos))
				return;

			for (var i = 0; i < supportPowerIconBounds.Count; i++)
			{
				if (!supportPowerIconBounds[i].Contains(Viewport.LastMousePos))
					continue;

				lastIconIdx = i;
				TooltipIcon = supportPowerIcons[i];
				currentTooltipToken = tooltipContainer.Value.SetTooltip(TooltipTemplate,
					new WidgetArgs() { { "world", worldRenderer.World }, { "player", GetPlayer() }, { "getTooltipIcon", GetTooltipIcon }, { "playerResources", GetPlayer().PlayerActor.Trait<PlayerResources>() } });
				return;
			}

			TooltipIcon = null;
		}
	}
}