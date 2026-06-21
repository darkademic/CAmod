#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Effects
{
	public class FlashTargetCA : IEffect
	{
		readonly Actor target;
		readonly int duration;
		readonly int count;
		readonly int interval;
		readonly int totalDuration;

		readonly TintModifiers modifiers;
		readonly float3 tint;
		readonly float? alpha;

		int tick;

		FlashTargetCA(Actor target, int duration, int count, int interval, int delay)
		{
			this.target = target;
			this.duration = Math.Max(1, duration);
			this.count = count;
			this.interval = Math.Max(0, interval);
			totalDuration = count <= 0 ? 0 : count * this.duration + (count - 1) * this.interval;
			tick = -delay;

			target.World.RemoveAll(effect => effect is FlashTargetCA flashTarget && flashTarget.target == target);
		}

		public FlashTargetCA(Actor target, Color color, float alpha = 0.5f, int duration = 2, int count = 2, int interval = 2, int delay = 0)
			: this(target, duration, count, interval, delay)
		{
			modifiers = TintModifiers.ReplaceColor;
			tint = new float3(color.R, color.G, color.B) / 255f;
			this.alpha = alpha;
		}

		public FlashTargetCA(Actor target, float3 tint, int duration = 2, int count = 2, int interval = 2, int delay = 0)
			: this(target, duration, count, interval, delay)
		{
			this.tint = tint;
		}

		public void Tick(World world)
		{
			if (++tick >= totalDuration || !target.IsInWorld)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (target.IsInWorld && tick >= 0 && tick < totalDuration)
			{
				var cycle = duration + interval;
				var flashTick = tick % cycle;

				if (flashTick < duration)
				{
					return target.Render(wr)
						.Where(r => !r.IsDecoration && r is IModifyableRenderable)
						.Select(r =>
						{
							var mr = (IModifyableRenderable)r;
							mr = mr.WithTint(tint, mr.TintModifiers | modifiers);
							if (alpha.HasValue)
							{
								var flashAlpha = alpha.Value * (duration - flashTick) / duration;
								mr = mr.WithAlpha(flashAlpha);
							}

							return mr;
						});
				}
			}

			return SpriteRenderable.None;
		}
	}
}
