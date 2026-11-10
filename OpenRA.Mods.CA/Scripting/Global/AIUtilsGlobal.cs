

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
using Eluant;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Scripting
{
	[ScriptGlobal("AIUtils")]
	public class AIUtilsGlobal : ScriptGlobal
	{
		readonly World world;

		public AIUtilsGlobal(ScriptContext context)
			: base(context)
		{
			world = context.World;
		}

		[Desc("Returns a cell according to target selection parameters.")]
		public CPos? FindTargetLocation(Player player, string[] targetTypes, int minimumValue, int coarseScanRadius, int fineScanRadius = 2)
		{
			if (targetTypes == null)
				throw new LuaException("Target types cannot be null.");

			if (coarseScanRadius <= 0 || fineScanRadius <= 0)
				throw new LuaException("Scan radii must be at least one cell.");

			if (player == null)
				throw new LuaException("Cannot find a target location without an active player.");

			var targetTypeSet = new BitSet<TargetableType>(targetTypes);
			var map = world.Map;
			var suitableLocations = new List<(MPos UV, int Value)>();
			var totalValue = 0;

			for (var i = 0; i < map.MapSize.X; i += coarseScanRadius)
			{
				for (var j = 0; j < map.MapSize.Y; j += coarseScanRadius)
				{
					var tl = new MPos(i, j);
					var br = new MPos(i + coarseScanRadius, j + coarseScanRadius);
					var region = new CellRegion(map.Grid.Type, tl, br);

					var wtl = map.CenterOfCell(tl.ToCPos(map));
					var wbr = map.CenterOfCell(br.ToCPos(map));
					var actors = world.ActorMap.ActorsInBox(wtl, wbr);
					var frozenActors = player.FrozenActorLayer != null
						? player.FrozenActorLayer.FrozenActorsInRegion(region, onlyVisible: false)
						: Enumerable.Empty<FrozenActor>();
					var value = GetTargetValue(actors, player, targetTypeSet) +
						GetTargetValue(frozenActors, player, targetTypeSet);

					if (value < minimumValue)
						continue;

					suitableLocations.Add((tl, value));
					totalValue += value;
				}
			}

			if (suitableLocations.Count == 0)
				return null;

			var averageValue = totalValue / suitableLocations.Count;
			var coarseLocation = suitableLocations.Shuffle(world.SharedRandom)
				.First(location => location.Value >= averageValue)
				.UV.ToCPos(map);

			CPos? bestLocation = null;
			var bestValue = 0;
			for (var i = -1; i <= coarseScanRadius + 1; i += fineScanRadius)
			{
				for (var j = -1; j <= coarseScanRadius + 1; j += fineScanRadius)
				{
					var location = new CPos(coarseLocation.X + i, coarseLocation.Y + j);
					if (!map.Contains(location))
						continue;

					var position = map.CenterOfCell(location);
					var value = GetTargetValueAt(position, player, targetTypeSet, WDist.FromCells(coarseScanRadius));
					if (value <= bestValue || value < minimumValue)
						continue;

					bestLocation = location;
					bestValue = value;
				}
			}

			if (!bestLocation.HasValue)
				return null;

			return bestLocation.Value;
		}

		static int GetTargetValue(IEnumerable<Actor> actors, Player player, BitSet<TargetableType> targetTypes)
		{
			var value = 0;
			foreach (var actor in actors)
			{
				if (player.RelationshipWith(actor.Owner) != PlayerRelationship.Enemy ||
					!actor.IsTargetableBy(player.PlayerActor) ||
					!targetTypes.Overlaps(actor.GetEnabledTargetTypes()))
					continue;

				value += actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			}

			return value;
		}

		static int GetTargetValue(IEnumerable<FrozenActor> actors, Player player, BitSet<TargetableType> targetTypes)
		{
			var value = 0;
			foreach (var actor in actors)
			{
				if (!actor.IsValid ||
					player.RelationshipWith(actor.Owner) != PlayerRelationship.Enemy ||
					!targetTypes.Overlaps(actor.TargetTypes))
					continue;

				value += actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			}

			return value;
		}

		int GetTargetValueAt(WPos position, Player player, BitSet<TargetableType> targetTypes, WDist radius)
		{
			var value = GetTargetValue(world.FindActorsInCircle(position, radius), player, targetTypes);
			if (player.FrozenActorLayer == null)
				return value;

			var delta = new WVec(radius, radius, WDist.Zero);
			var tl = world.Map.CellContaining(position - delta);
			var br = world.Map.CellContaining(position + delta);
			var region = new CellRegion(world.Map.Grid.Type, tl, br);
			return value + GetTargetValue(player.FrozenActorLayer.FrozenActorsInRegion(region), player, targetTypes);
		}
    }
}
