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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Shows matching range circles from existing actors while placing a structure.")]
	sealed class WithLinkedRangeCirclePreviewInfo : TraitInfo<WithLinkedRangeCirclePreview>, IPlaceBuildingDecorationInfo
	{
		[Desc("Type of linked range circle to render from existing actors.")]
		public readonly string Type = null;

		public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World w, ActorInfo ai, WPos centerPosition)
		{
			if (string.IsNullOrEmpty(Type))
				yield break;

			foreach (var a in w.ActorsWithTrait<WithRangeCircleCA>())
				if (a.Trait.Info.Type == Type)
					foreach (var r in a.Trait.RenderRangeCircle(a.Actor, RangeCircleVisibility.WhenSelected))
						yield return r;
		}
	}

	public class WithLinkedRangeCirclePreview { }
}