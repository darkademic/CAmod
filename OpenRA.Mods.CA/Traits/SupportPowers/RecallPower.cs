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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	sealed class RecallPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("Range in which to apply condition.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("Minimum targets for power to activate.")]
		public readonly int MinTargets = 1;

		[Desc("Maximum number of targets. Zero for no limit.")]
		public readonly int MaxTargets = 0;

		[Desc("Maximum number of enemy targets. Zero for no limit (MaxTargets still applies).")]
		public readonly int MaxEnemyTargets = 0;

		[Desc("If true, keeps formation of teleported units.")]
		public readonly bool KeepFormation = false;

		public readonly bool KillCargo = true;

		[Desc("Target types that can be recalled.")]
		public readonly BitSet<TargetableType> ValidTargetTypes = default(BitSet<TargetableType>);

		[Desc("Target types that cannot be recalled.")]
		public readonly BitSet<TargetableType> InvalidTargetTypes = default(BitSet<TargetableType>);

		[Desc("Player relationships that can be recalled.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[CursorReference]
		[Desc("Cursor to display when the targeted area is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		[Desc("Font to use for target count.")]
		public readonly string TargetCountFont = "Medium";

		public readonly bool ShowSelectionBoxes = false;
		public readonly Color SelectionBoxColor = Color.White;

		[Desc("Target tint colour.")]
		public readonly Color? TargetTintColor = null;

		public readonly bool ShowTargetCircle = false;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;

		[Desc("Warp from sequence sprite image.")]
		public readonly string WarpFromImage = null;

		[Desc("Warp from sequence.")]
		[SequenceReference(nameof(WarpFromImage))]
		public readonly string WarpFromSequence = null;

		[Desc("Warp to sequence sprite image.")]
		public readonly string WarpToImage = null;

		[Desc("Warp to sequence.")]
		[SequenceReference(nameof(WarpToImage))]
		public readonly string WarpToSequence = null;

		[PaletteReference]
		public readonly string WarpEffectPalette = "effect";

		public override object Create(ActorInitializer init) { return new RecallPower(init.Self, this); }
	}

	sealed class RecallPower : SupportPower
	{
		readonly RecallPowerInfo info;

		public RecallPower(Actor self, RecallPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectRecallTarget(Self.World, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var info = (RecallPowerInfo)Info;
			var targetCell = self.World.Map.CellContaining(order.Target.CenterPosition);
			var recallCell = self.Location;
			var recallPos = self.World.Map.CenterOfCell(recallCell);

			var actorsToTeleport = GetTargets(targetCell);

			var targetDelta = recallCell - targetCell;

			foreach (var actor in actorsToTeleport)
			{
				var destinationCell = info.KeepFormation ? actor.Location + targetDelta : recallCell;

				if (self.Owner.Shroud.IsExplored(destinationCell))
					actor.QueueActivity(false, new TeleportCA(Self, destinationCell, null, info.KillCargo, false, null));
			}

			if (info.WarpFromImage != null && info.WarpFromSequence != null)
				self.World.Add(new SpriteEffect(recallPos, self.World, info.WarpFromImage, info.WarpFromSequence, info.WarpEffectPalette));

			if (info.WarpToImage != null && info.WarpToSequence != null)
				self.World.Add(new SpriteEffect(order.Target.CenterPosition, self.World, info.WarpToImage, info.WarpToSequence, info.WarpEffectPalette));
		}

		public IEnumerable<Actor> GetTargets(CPos xy)
		{
			var centerPos = Self.World.Map.CenterOfCell(xy);

			var actorsInRange = Self.World.FindActorsInCircle(centerPos, info.Range)
				.Where(a => IsValidTarget(a))
				.OrderBy(a => (a.CenterPosition - centerPos).LengthSquared);

			// If we have a target limit
			if (info.MaxTargets > 0)
			{
				// If no enemy target limit, or the overall target limit is lower
				if (info.MaxEnemyTargets == 0 || info.MaxTargets < info.MaxEnemyTargets)
					return actorsInRange.Take(info.MaxTargets);
				else
				{
					var targets = new List<Actor>();
					var enemyTargets = 0;

					foreach (var a in actorsInRange)
					{
						if (info.MaxTargets > 0 && targets.Count() >= info.MaxTargets)
							break;

						if (info.MaxEnemyTargets > 0)
						{
							var isEnemy = !a.Owner.IsAlliedWith(Self.Owner);

							if (isEnemy && enemyTargets >= info.MaxEnemyTargets)
								continue;

							if (isEnemy)
								enemyTargets++;
						}

						targets.Add(a);
					}

					return targets;
				}
			}
			else
				return actorsInRange;
		}

		public bool IsValidTarget(Actor a)
		{
			if (a == null || !a.IsInWorld || a.IsDead)
				return false;

			if (!info.ValidRelationships.HasRelationship(Self.Owner.RelationshipWith(a.Owner)))
				return false;

			var targetTypes = a.GetEnabledTargetTypes();

			if (!targetTypes.Overlaps(info.ValidTargetTypes))
				return false;

			if (targetTypes.Overlaps(info.InvalidTargetTypes))
				return false;

			if (!Self.Owner.Shroud.IsVisible(a.Location))
				return false;

			if (!a.CanBeViewedByPlayer(Self.Owner))
				return false;

			return true;
		}

		sealed class SelectRecallTarget : OrderGenerator
		{
			readonly RecallPower power;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectRecallTarget(World world, string order, SupportPowerManager manager, RecallPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;

				var info = (RecallPowerInfo)power.Info;
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				var targets = power.GetTargets(cell);
				if (mi.Button == MouseButton.Left && targets.Count() >= power.info.MinTargets)
					yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var targetUnits = power.GetTargets(xy);

				if (power.info.ShowSelectionBoxes)
				{
					foreach (var unit in targetUnits)
					{
						var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
						if (decorations != null)
							foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, power.info.SelectionBoxColor))
								yield return d;
					}
				}

				if (power.info.ShowTargetCircle)
				{
					yield return new RangeCircleAnnotationRenderable(
						world.Map.CenterOfCell(xy),
						power.info.Range,
						0,
						power.info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.info.TargetCircleColor,
						1,
						Color.FromArgb(96, Color.Black),
						3);
				}

				if (power.info.MaxTargets > 0)
				{
					var font = Game.Renderer.Fonts[power.info.TargetCountFont];
					var color = power.info.TargetCircleColor;
					var text = targetUnits.Count() + " / " + power.info.MaxTargets;
					var size = font.Measure(text);
					var textPos = new int2(Viewport.LastMousePos.X - (size.X / 2), Viewport.LastMousePos.Y - (size.Y * 2) - (size.Y / 3));
					yield return new UITextRenderable(font, WPos.Zero, textPos, 0, color, text);
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

				if (power.info.TargetTintColor != null)
				{
					var targetUnits = power.GetTargets(xy);

					foreach (var unit in targetUnits)
					{
						var renderables = unit.Render(wr)
							.Where(r => !r.IsDecoration && r is IModifyableRenderable)
							.Select(r =>
							{
								var mr = (IModifyableRenderable)r;
								var tint = new float3(power.info.TargetTintColor.Value.R, power.info.TargetTintColor.Value.G, power.info.TargetTintColor.Value.B) / 255f;
								mr = mr.WithTint(tint, mr.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(power.info.TargetTintColor.Value.A / 255f);
								return mr;
							});

						foreach (var r in renderables)
						{
							yield return r;
						}
					}
				}
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return ((RecallPowerInfo)power.Info).Cursor;
			}
		}
	}
}
