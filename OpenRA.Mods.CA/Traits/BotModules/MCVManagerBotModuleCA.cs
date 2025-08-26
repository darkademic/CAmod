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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum BotMcvExpansionMode { CheckResourceCreator, CheckResource, CheckBase }

	[Desc("Manages AI MCVs and expansion.")]
	public class McvManagerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are considered MCVs (deploy into base builders).")]
		public readonly HashSet<string> McvTypes = new();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new();

		[Desc("Actor types that are able to produce MCVs.")]
		public readonly HashSet<string> McvFactoryTypes = new();

		[Desc("Try to maintain at least this many ConstructionYardTypes, build an MCV if number is below this.")]
		public readonly int MinimumConstructionYardCount = 1;

		[Desc("Delay (in ticks) between looking for and giving out orders to new MCVs.")]
		public readonly int ScanForNewMcvInterval = 20;

		[Desc("Delay (in ticks) between check and build a MCV.")]
		public readonly int BuildMcvInterval = 101;

		[Desc("Move a conyard if have more than 1 conyard, for better expansion ")]
		public readonly int MoveConyardTick = 4500;

		[Desc("Tells the AI what building types are considered production.")]
		public readonly HashSet<string> ProductionTypes = new();

		[Desc("Tells the AI what building types are considered refineries.")]
		public readonly HashSet<string> RefineryTypes = new();

		[Desc("Move a conyard even when this is the only conyard, only works for once ")]
		public readonly int[] FirstMoveConyardTicks = { -1 };

		[Desc("Initial expasion mode choosed by AI.")]
		public readonly BotMcvExpansionMode InitialExpansionMode = BotMcvExpansionMode.CheckResourceCreator;

		/* those are options shared by two or more modes */
		[Desc("Tick in update an indice of resource map.")]
		public readonly int UpdateResourceMapInverval = 83;

		[Desc("Distance in cells of indice of resource map.")]
		public readonly int ResourceMapStride = 18;

		/* those are CheckResourceCreator mode options */
		[Desc("Minimum distance in cells around the resource creator location when checking for MCV deployment location.")]
		public readonly int CRCmodeMinDeployRadius = 2;

		[Desc("Maximum distance in cells around the resource creator location when checking for MCV deployment location.")]
		public readonly int CRCmodeMaxDeployRadius = 12;

		[Desc("Tells the AI what types are considered resource creator.")]
		public readonly HashSet<string> ResourceCreatorTypes = new();

		[Desc("Distance in cells to a friendly conyard that AI dislike when choose a expanding location.")]
		public readonly int CRCmodeConyardUnfavorRange = 18;

		[Desc("Distance in cells to a friendly refinery that AI dislike when choose a expanding location.")]
		public readonly int CRCmodeRefineryUnfavorRange = 12;

		[Desc("Distance in cells that AI try to maintain to the expanding location in deployment.")]
		public readonly int CRCmodeTryMaintainRange = 8;

		[Desc("Distance in cells from center of the base when checking near by enemies for MCV expanding location.")]
		public readonly int CRCmodeEnemyScanRadius = 10;

		/* those are CheckResource mode options */
		[Desc("Minimum distance in cells from the found resource creator location when checking for MCV deployment location.")]
		public readonly int CRmodeMinDeployRadius = 2;

		[Desc("Maximum distance in cells the found resource creator location when checking for MCV deployment location.")]
		public readonly int CRmodeMaxDeployRadius = 20;

		[Desc("Distance in cells that AI try to maintain to the expanding location in deployment.")]
		public readonly int CRmodeTryMaintainRange = 8;

		[Desc("Resource types that are considered can be harvested.")]
		public readonly HashSet<string> ValidResourceTypes = new();

		/* those are CheckBase mode options */
		[Desc("Minimum distance in cells from center of the base expansion when checking for MCV deployment location.")]
		public readonly int CBmodeMinDeployRadius = 2;

		[Desc("Maximum distance in cells from center of the base expansion when checking for MCV deployment location.")]
		public readonly int CBmodeMaxDeployRadius = 20;

		public override object Create(ActorInitializer init) { return new McvManagerBotModuleCA(init.Self, this); }
	}

	public class McvManagerBotModuleCA : ConditionalTrait<McvManagerBotModuleCAInfo>, IBotTick, IBotRespondToAttack
	{
		const int PositiveMaxFailedAttempts = 3;
		const int NegativeMaxFailedAttempts = 1;

		readonly World world;
		readonly Player player;
		readonly ActorIndex.OwnerAndNamesAndTrait<TransformsInfo> mcvs;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> constructionYards;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> mcvFactories;
		readonly int resourceIndiceStride;

		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotRequestUnitProduction[] requestUnitProduction;

		int scanInterval;
		int buildMCVInterval;
		int moveConyardInterval;
		int updateResourceMapInterval;
		bool firstTick = true;
		bool firstUndeploy = true;
		bool allowfallback;

		BotMcvExpansionMode mcvExpansionMode;
		int mcvDeploymentMinDeployRadius;
		int mcvDeploymentMaxDeployRadius;
		int mcvDeploymentTryMaintainRange;

		int maxFailedAttempts = 3;
		int failedAttempts;
		CPos? lastFailedExpandSpot;

		int attackrespondcooldown = 20;

		int pathDistanceSquareFactor;

		BitArray resourceTypeIndices = null;
		(CPos Center, int Value)[] resourceMapIndices = null;
		int resourceMapIndicesColumnCount;
		int resourceMapIndicesRowCount;
		int updateResourceMapIndex;

		public McvManagerBotModuleCA(Actor self, McvManagerBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			mcvs = new ActorIndex.OwnerAndNamesAndTrait<TransformsInfo>(world, info.McvTypes, player);
			constructionYards = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			mcvFactories = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.McvFactoryTypes, player);
			resourceIndiceStride = info.ResourceMapStride;
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			notifyPositionsUpdated = self.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			requestUnitProduction = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			moveConyardInterval = Info.FirstMoveConyardTicks.RandomOrDefault(world.LocalRandom);
			if (moveConyardInterval == -1)
				firstUndeploy = false;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			scanInterval = world.LocalRandom.Next(Info.ScanForNewMcvInterval, Info.ScanForNewMcvInterval << 1);
			buildMCVInterval = world.LocalRandom.Next(Info.BuildMcvInterval, Info.BuildMcvInterval << 1);
			updateResourceMapInterval = world.LocalRandom.Next(Info.UpdateResourceMapInverval, Info.UpdateResourceMapInverval << 1);

			var map = world.Map;
			var ti = map.Rules.TerrainInfo;

			if (resourceTypeIndices == null)
			{
				resourceTypeIndices = new BitArray(ti.TerrainTypes.Length);
				foreach (var i in world.Map.Rules.Actors["world"].TraitInfos<ResourceLayerInfo>())
				{
					foreach (var t in i.ResourceTypes)
					{
						if (Info.ValidResourceTypes.Contains(t.Value.TerrainType))
							resourceTypeIndices.Set(ti.GetTerrainIndex(t.Value.TerrainType), true);
					}
				}
			}

			if (resourceMapIndices == null)
			{
				resourceMapIndicesColumnCount = (map.MapSize.X + resourceIndiceStride - 1) / resourceIndiceStride;
				resourceMapIndicesRowCount = (map.MapSize.Y + resourceIndiceStride - 1) / resourceIndiceStride;
				resourceMapIndices = Exts.MakeArray(resourceMapIndicesColumnCount * resourceMapIndicesRowCount, i => (new MPos(
					i % resourceMapIndicesColumnCount * resourceIndiceStride + (resourceIndiceStride >> 1),
					i / resourceMapIndicesRowCount * resourceIndiceStride + (resourceIndiceStride >> 1)).ToCPos(map), 0)).Shuffle(world.LocalRandom).ToArray();
				for (var i = 0; i < resourceMapIndices.Length; i++)
					UpdateResourceMap(i);

				pathDistanceSquareFactor = resourceMapIndicesColumnCount * resourceMapIndicesColumnCount + resourceMapIndicesRowCount * resourceMapIndicesRowCount;
			}
		}

		void SwitchExpansionMode(BotMcvExpansionMode nextMode)
		{
			mcvExpansionMode = nextMode;
			switch (nextMode)
			{
				case BotMcvExpansionMode.CheckResourceCreator:
					mcvDeploymentMinDeployRadius = Info.CRmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CRmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = Info.CRCmodeTryMaintainRange;
					break;

				case BotMcvExpansionMode.CheckResource:
					mcvDeploymentMinDeployRadius = Info.CBmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CBmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = Info.CRmodeTryMaintainRange;
					break;

				case BotMcvExpansionMode.CheckBase:
					mcvDeploymentMinDeployRadius = Info.CBmodeMinDeployRadius;
					mcvDeploymentMaxDeployRadius = Info.CBmodeMaxDeployRadius;
					mcvDeploymentTryMaintainRange = (Info.CBmodeMaxDeployRadius + Info.CBmodeMinDeployRadius) >> 1;
					break;

				default:
					break;
			}
		}

		void FindBadDeploySpot(CPos? failedSpot)
		{
			lastFailedExpandSpot = failedSpot.Value;
			if (++failedAttempts >= maxFailedAttempts)
			{
				failedAttempts = 0;
				switch (mcvExpansionMode)
				{
					case BotMcvExpansionMode.CheckResourceCreator:
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
						break;

					case BotMcvExpansionMode.CheckResource:
						SwitchExpansionMode(BotMcvExpansionMode.CheckBase);
						break;

					case BotMcvExpansionMode.CheckBase:
						SwitchExpansionMode(BotMcvExpansionMode.CheckResourceCreator);
						maxFailedAttempts = NegativeMaxFailedAttempts;
						break;
				}
			}
		}

		void FindGoodDeploySpot()
		{
			lastFailedExpandSpot = null;
			if (--failedAttempts <= -maxFailedAttempts)
			{
				maxFailedAttempts = PositiveMaxFailedAttempts;
				switch (mcvExpansionMode)
				{
					case BotMcvExpansionMode.CheckResourceCreator:
						failedAttempts = -maxFailedAttempts;
						break;

					case BotMcvExpansionMode.CheckBase:
						failedAttempts = maxFailedAttempts - 1;
						SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
						break;

					case BotMcvExpansionMode.CheckResource:
						failedAttempts = maxFailedAttempts - 1;
						SwitchExpansionMode(BotMcvExpansionMode.CheckResourceCreator);
						break;
				}
			}
		}

		public (CPos? ExpandLocation, int Attraction) GetExpandCenter(CPos mcv_loc, bool allowfallback)
		{
			/*
			 * The following codes find expansion point as the important reference for current MCV's deployment:
			 * 1. For CheckBase and CheckResource modes, resourceMapIndices is used to find the best indice for the expansion point.
			 * 2. For CheckResourceCreator mode, all resource creator locations are considered as candidates, and the best one is chosen as the expansion point.
			 *
			 * indiceStrideSquare (which is equal to resourceIndiceStride * resourceIndiceStride) is used as the basic unit to calculate the attraction of a candidate,
			 * so we can compare the attraction on the same scale on different factors, such as candidate's distance to current MCV and ally construction yard & refinery within range, etc:
			 *
			 * 1). the weight of candidate's distance-square to current MCV: range from 0 to -indiceStrideSquare.
			 *
			 *     The reason why:
			 *
			 *     It is calculated as "(candidate - mcv_loc).LengthSquared / pathDistanceSquareFactor".
			 *     noted that: pathDistanceSquareFactor = resourceMapIndicesColumnCount * resourceMapIndicesColumnCount + resourceMapIndicesRowCount * resourceMapIndicesRowCount,
			 *
			 *     Consider a map, we divide it at the length of resourceIndiceStride = r, and then its resourceMapIndicesColumnCount = a, resourceMapIndicesRowCount = b,
			 *     so the map.width ≈ a*r, map.height ≈ b*r,
			 *     the maximum distance-square between two points on the map is (a*r)(a*r) + (b*r)(b*r),
			 *     so the maximum "weight of candidate's distance to current MCV" is from 0 to -((a*r)(a*r) + (b*r)(b*r)) / (a*a + b*b) = -r*r = -indiceStrideSquare.
			 *
			 * 2). the weight of friendly construction yard within range: -indiceStrideSquare. If it belongs to an ally, -indiceStrideSquare/2.
			 *
			 * 3). the weight of enemy building within range: -indiceStrideSquare*4.
			 *
			 * 4). the weight of friendly refinery within range (not for CheckBase mode): -indiceStrideSquare. If it belongs to an ally, -indiceStrideSquare/2.
			 *
			 * 5). the weight of resource amount (only for CheckResource mode): from 0 to +indiceStrideSquare/2.
			 *
			 *     The reason why:
			 *
			 *     The maximum resource amount in a stride of resource map is proximately indiceStrideSquare (full of it), but an stride full of resource is less likely
			 *     have room for buildings. so the we perfer only the half of the stride full of resource the most, which may give us enough room to place buildings.
			 *
			 *     so the weight can be: (indiceStrideSquare/4) - |(strideValue - (resourceIndiceStride/2)) * resourceIndiceStride/2|
			 *
			 */
			var indiceStrideSquare = resourceIndiceStride * resourceIndiceStride;
			switch (mcvExpansionMode)
			{
				/*
				 * CheckBase mode only considers the distance to current MCV, ally construction yard within range and enemy buildings within range.
				 * Attaction has a base value of indiceStrideSquare >> 3 (1/8 of the maximum distance weight, 1/(2*sqrt(2))≈ 1/2.8 of the maximum distance in map)
				 */
				case BotMcvExpansionMode.CheckBase:
					var cb_conyardlocs = world.ActorsHavingTrait<Building>().Where(a => a.Owner.IsAlliedWith(player)
						&& Info.ConstructionYardTypes.Contains(a.Info.Name)).Select(a => (a.Location, a.Owner == player)).ToArray();

					CPos? cb_suitablespot = null;
					var cb_best = int.MinValue;

					foreach (var (center, value) in resourceMapIndices)
					{
						var attraction = indiceStrideSquare >> 3;

						attraction -= (center - mcv_loc).LengthSquared / pathDistanceSquareFactor;

						if (lastFailedExpandSpot == center)
							continue;

						if (world.FindActorsInCircle(world.Map.CenterOfCell(center), WDist.FromCells(resourceIndiceStride)).Any(a => !a.Disposed
							&& (player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
							&& a.Info.HasTraitInfo<BuildingInfo>()
							&& a.Info.HasTraitInfo<SellableInfo>()))
							attraction -= indiceStrideSquare << 2;

						foreach (var (location, isAlly) in cb_conyardlocs)
						{
							var sdistance = (center - location).LengthSquared;
							if (sdistance <= indiceStrideSquare)
							{
								if (isAlly)
									attraction -= indiceStrideSquare;
								else
									attraction -= indiceStrideSquare << 1;
							}
						}

						if (!allowfallback)
						{
							var sdistance = (center - mcv_loc).LengthSquared;
							if (sdistance <= indiceStrideSquare)
								attraction -= indiceStrideSquare << 1;
						}

						if (attraction > cb_best)
						{
							cb_best = attraction;
							cb_suitablespot = center;
						}
					}

					return (cb_suitablespot ?? mcv_loc, cb_best);

				/*
				 * CheckResourceCreator mode considers the distance to current MCV, ally construction yard & refinery within range,
				 * Attaction has a base value of indiceStrideSquare >> 4 (1/16 of the maximum distance weight, 1/4 maximum map distance in map).
				 * Because resource amount is also a factor to be considered compared to other modes, so the base attaction is reduced by half.
				 */
				case BotMcvExpansionMode.CheckResource:

					var cr_refinarylocs = world.ActorsHavingTrait<Refinery>().Where(a => a.Owner == player && Info.RefineryTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner != player))
						.ToArray();

					var cr_conyardlocs = world.ActorsHavingTrait<Building>().Where(a => a.Owner.IsAlliedWith(player)
						&& Info.ConstructionYardTypes.Contains(a.Info.Name)).Select(a => (a.Location, a.Owner != player)).ToArray();

					CPos? cr_suitablespot = null;
					var cr_best = int.MinValue;

					foreach (var (center, value) in resourceMapIndices)
					{
						// don't have to look into cell with no resource
						if (value == 0)
							continue;

						var attraction = indiceStrideSquare >> 4;

						// it is better that resource cells takes only half of the indice cells, which give us the place to place building.
						attraction += (indiceStrideSquare >> 2) - (Math.Abs(value - (resourceIndiceStride >> 1)) * resourceIndiceStride >> 1);

						attraction -= (center - mcv_loc).LengthSquared / pathDistanceSquareFactor;

						if (world.FindActorsInCircle(world.Map.CenterOfCell(center), WDist.FromCells(resourceIndiceStride)).Any(a => !a.Disposed
							&& (player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
							&& a.Info.HasTraitInfo<BuildingInfo>()
							&& a.Info.HasTraitInfo<SellableInfo>()))
							attraction -= indiceStrideSquare;

						foreach (var (location, isAlly) in cr_refinarylocs)
						{
							var sdistance = (center - location).LengthSquared;
							if (sdistance <= indiceStrideSquare)
							{
								if (isAlly)
									attraction -= indiceStrideSquare;
								else
									attraction -= indiceStrideSquare << 1;
							}
						}

						foreach (var (location, isAlly) in cr_conyardlocs)
						{
							var sdistance = (center - location).LengthSquared;
							if (sdistance <= indiceStrideSquare)
							{
								if (isAlly)
									attraction -= indiceStrideSquare;
								else
									attraction -= indiceStrideSquare << 1;
							}
						}

						if (!allowfallback)
						{
							var sdistance = (center - mcv_loc).LengthSquared;
							if (sdistance <= indiceStrideSquare)
								attraction -= indiceStrideSquare << 1;
						}

						if (attraction > cr_best)
						{
							cr_best = attraction;
							cr_suitablespot = center;
						}
					}

					var resourceloc = world.Map.FindTilesInAnnulus(cr_suitablespot.Value, mcvDeploymentMaxDeployRadius, world.Map.Grid.MaximumTileSearchRange)
						.Where(a => a != lastFailedExpandSpot && resourceTypeIndices.Get(world.Map.GetTerrainIndex(a))).RandomOrDefault(world.LocalRandom);

					return (resourceloc, cr_best);

				/*
				 * CheckResourceCreator mode considers the distance to current MCV, ally construction yard & refinery within range,
				 * Attaction has a base value of indiceStrideSquare >> 3 (1/8 of the maximum distance weight, 1/(2*sqrt(2))≈ 1/2.8 of the maximum distance in map)
				 */
				case BotMcvExpansionMode.CheckResourceCreator:
					var crc_conyardlocs = world.ActorsHavingTrait<Building>().Where(a => a.Owner.IsAlliedWith(player)
						&& Info.ConstructionYardTypes.Contains(a.Info.Name)).Select(a => (a.Location, a.Owner != player)).ToArray();

					var crc_refinarylocs = world.ActorsHavingTrait<Refinery>().Where(a => a.Owner.IsAlliedWith(player) && Info.RefineryTypes.Contains(a.Info.Name))
						.Select(a => (a.Location, a.Owner != player))
						.ToArray();

					var crc_rescreators = world.ActorsHavingTrait<SeedsResourceCA>().Where(a => Info.ResourceCreatorTypes.Contains(a.Info.Name));

					CPos? crc_suitablelocation = null;
					var crc_best = int.MinValue;

					foreach (var rescreator in crc_rescreators)
					{
						var attraction = indiceStrideSquare >> 3;

						attraction -= (rescreator.Location - mcv_loc).LengthSquared / pathDistanceSquareFactor;

						if (lastFailedExpandSpot == rescreator.Location)
							continue;

						if (world.FindActorsInCircle(rescreator.CenterPosition, WDist.FromCells(Info.CRCmodeEnemyScanRadius)).Any(a => !a.Disposed
							&& (player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
							&& a.Info.HasTraitInfo<BuildingInfo>()
							&& a.Info.HasTraitInfo<SellableInfo>()))
							attraction -= indiceStrideSquare << 2;

						foreach (var (location, isAlly) in crc_refinarylocs)
						{
							var sdistance = (rescreator.Location - location).LengthSquared;
							if (sdistance <= Info.CRCmodeRefineryUnfavorRange * Info.CRCmodeRefineryUnfavorRange)
							{
								if (isAlly)
									attraction -= indiceStrideSquare;
								else
									attraction -= indiceStrideSquare << 1;
							}
						}

						foreach (var (location, isAlly) in crc_conyardlocs)
						{
							var sdistance = (rescreator.Location - location).LengthSquared;
							if (sdistance <= Info.CRCmodeConyardUnfavorRange * Info.CRCmodeConyardUnfavorRange)
							{
								if (isAlly)
									attraction -= indiceStrideSquare;
								else
									attraction -= indiceStrideSquare << 1;
							}
						}

						if (!allowfallback)
						{
							var sdistance = (rescreator.Location - mcv_loc).LengthSquared;
							if (sdistance <= indiceStrideSquare)
								attraction -= indiceStrideSquare << 1;
						}

						if (attraction > crc_best)
						{
							crc_best = attraction;
							crc_suitablelocation = rescreator.Location;
						}
					}

					return (crc_suitablelocation, crc_best);

				default:
					return (null, int.MinValue);
			}
		}

		void IBotTick.BotTick(IBot bot)
		{
			attackrespondcooldown--;

			if (firstTick)
			{
				// check which mode we should use in map
				SwitchExpansionMode(Info.InitialExpansionMode);
				if (mcvExpansionMode != BotMcvExpansionMode.CheckBase && !world.ActorsHavingTrait<SeedsResourceCA>().Any())
				{
					SwitchExpansionMode(BotMcvExpansionMode.CheckResource);
					if (!world.Map.AllCells.Any(a => resourceTypeIndices.Get(world.Map.GetTerrainIndex(a))))
						SwitchExpansionMode(BotMcvExpansionMode.CheckBase);
				}

				DeployMcvs(bot, false);
				firstTick = false;
			}

			if (--scanInterval <= 0)
			{
				scanInterval = Info.ScanForNewMcvInterval;
				DeployMcvs(bot, true);
			}

			if (--buildMCVInterval <= 0)
			{
				buildMCVInterval = Info.BuildMcvInterval;
				BuildMCV(bot);
			}

			if (--updateResourceMapInterval <= 0)
			{
				updateResourceMapInterval = Info.UpdateResourceMapInverval;
				UpdateResourceMap(updateResourceMapIndex);
				updateResourceMapIndex = (updateResourceMapIndex + 1) % resourceMapIndices.Length;
			}

			if (--moveConyardInterval <= 0)
			{
				moveConyardInterval = Info.MoveConyardTick;
				UnDeployConyard(bot);
			}
		}

		void UpdateResourceMap(int index)
		{
			resourceMapIndices[index] = (resourceMapIndices[index].Center, world.Map.FindTilesInAnnulus(resourceMapIndices[index].Center, 0, resourceIndiceStride)
				.Count(a => resourceTypeIndices.Get(world.Map.GetTerrainIndex(a))));
		}

		void BuildMCV(IBot bot)
		{
			var conyardNum = AIUtils.CountActorByCommonName(constructionYards);
			var mcvNum = AIUtils.CountActorByCommonName(mcvs);

			// Only build MCV if we have no mcv in the field (make it an exception if have no conyard),
			// don't have one in production and don't have the desired number of construction yards
			if ((conyardNum <= 0 && mcvNum > 1) || (conyardNum > 0 && mcvNum > 0)
				|| conyardNum + mcvNum >= Info.MinimumConstructionYardCount || AIUtils.CountActorByCommonName(mcvFactories) <= 0
				|| mcvFactories.Actors.Any(a => !a.IsDead && a.TraitsImplementing<ProductionQueue>().Any(t => t.Enabled && t.AllQueued()
				.Any(q => Info.McvTypes.Contains(q.Item)))) || player.PlayerActor.TraitsImplementing<ProductionQueue>().Any(t => t.Enabled && t.AllQueued()
				.Any(q => Info.McvTypes.Contains(q.Item))) || Info.McvTypes.Count <= 0)
				return;

			var unitBuilder = requestUnitProduction.FirstEnabledTraitOrDefault();
			if (unitBuilder == null)
				return;
			var mcvType = Info.McvTypes.Random(world.LocalRandom);
			if (unitBuilder.RequestedProductionCount(bot, mcvType) <= 0)
				unitBuilder.RequestUnitProduction(bot, mcvType);
		}

		void DeployMcvs(IBot bot, bool chooseLocation)
		{
			var newMCVs = world.ActorsHavingTrait<Transforms>()
				.Where(a => a.Owner == player && a.IsIdle && Info.McvTypes.Contains(a.Info.Name));

			foreach (var mcv in newMCVs)
				DeployMcv(bot, mcv, chooseLocation);
		}

		void UnDeployConyard(IBot bot)
		{
			if (firstUndeploy)
			{
				if (world.ActorsHavingTrait<Building>().Count(a => a.Owner == player && Info.ProductionTypes.Contains(a.Info.Name)) >= 2
					&& world.ActorsHavingTrait<Refinery>().Any(a => a.Owner == player && Info.RefineryTypes.Contains(a.Info.Name)))
				{
					var idleconyards = constructionYards.Actors.Where(a => !a.IsDead).ToList();

					if (idleconyards.Count > 0)
					{
						bot.QueueOrder(new Order("DeployTransform", idleconyards[0], true));
						allowfallback = false;
					}
				}

				firstUndeploy = false;
				moveConyardInterval = world.LocalRandom.Next(Info.MoveConyardTick, Info.MoveConyardTick * 2);
			}
			else
			{
				var idleconyards = constructionYards.Actors
					.Where(a => !a.IsDead && !a.TraitsImplementing<ProductionQueue>()
					.Any(t => t.Enabled && t.AllQueued().Any(q => Info.RefineryTypes.Contains(q.Item))))
					.ToList();

				if (idleconyards.Count > 1)
					bot.QueueOrder(new Order("DeployTransform", idleconyards[0], true));
			}
		}

		// Find any MCV and deploy them at a sensible location.
		void DeployMcv(IBot bot, Actor mcv, bool move)
		{
			if (move)
			{
				var transformsInfo = mcv.Info.TraitInfo<TransformsInfo>();
				var desiredLocation = ChooseMcvDeployLocation(mcv, transformsInfo.IntoActor, transformsInfo.Offset, allowfallback);
				if (desiredLocation == null)
					return;

				bot.QueueOrder(new Order("Move", mcv, Target.FromCell(world, desiredLocation.Value), true));

				allowfallback = true;

				if (constructionYards.Actors.Any(a => !a.IsDead))
				{
					foreach (var n in notifyPositionsUpdated)
					{
						n.UpdatedBaseCenter(desiredLocation.Value);
						n.UpdatedDefenseCenter(desiredLocation.Value);
					}
				}
			}

			bot.QueueOrder(new Order("DeployTransform", mcv, true));
		}

		CPos? ChooseMcvDeployLocation(Actor mcv, string actorType, CVec offset, bool allowfallback)
		{
			var actorInfo = world.Map.Rules.Actors[actorType];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			// Find the buildable cell that is closest to pos and centered around center
			CPos? FindDeployCell(CPos? centerCell, CPos? targetCell, int minRange, int maxRange, int tryMaintainRange)
			{
				if (!centerCell.HasValue || !targetCell.HasValue)
					return null;

				var target = targetCell.Value;
				var center = centerCell.Value;

				var cells = world.Map.FindTilesInAnnulus(target, minRange, maxRange);

				/* First, sort the cells that keep tryMaintainRange to target (meanwhile direction is from center to target) the first to be considered
				 * by using following code. The idea is to use a linear combination of two distances-square for sorting weight.
				 *
				 * See comments in https://github.com/OpenRA/OpenRA/pull/22035 for explaination.
				 */
				if (center != target)
				{
					var theta = tryMaintainRange;
					var deta = (target - center).Length - tryMaintainRange;
					cells = cells.OrderBy(c => deta * (c - target).LengthSquared + theta * (c - center).LengthSquared);
				}
				else
					cells = cells.Shuffle(world.LocalRandom);

				CPos? bestcell = null;
				foreach (var cell in cells)
				{
					if (world.CanPlaceBuilding(cell + offset, actorInfo, bi, null))
					{
						bestcell = cell;
						break;
					}
				}

				if (bestcell == null)
					return null;

				// If the best cell is not ideal ( >= tryMaintainRange + 2), which means there might be some huge blockers
				// so we fall back to default behavior, which is the directly closest cell to target
				if (center != target && (bestcell.Value - target).LengthSquared >= (tryMaintainRange + 2) * (tryMaintainRange + 2))
				{
					cells = cells.OrderBy(c => (c - target).LengthSquared);
					foreach (var cell in cells)
					{
						if (world.CanPlaceBuilding(cell + offset, actorInfo, bi, null))
							return (cell - target).LengthSquared < (bestcell.Value - target).LengthSquared ? cell : bestcell;
					}
				}

				return bestcell;
			}

			var (expandCenter, attraction) = GetExpandCenter(mcv.Location, allowfallback);

			var bc = FindDeployCell(mcv.Location, expandCenter, mcvDeploymentMinDeployRadius, mcvDeploymentMaxDeployRadius, mcvDeploymentTryMaintainRange);

			if (bc.HasValue && attraction > 0)
				FindGoodDeploySpot();
			else
				FindBadDeploySpot(expandCenter);

			return bc.Value;
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (attackrespondcooldown <= 0 && Info.McvTypes.Contains(self.Info.Name))
			{
				attackrespondcooldown = 20;
				bot.QueueOrder(new Order("DeployTransform", self, false));
				if (AIUtils.CountActorByCommonName(constructionYards) == 0)
				{
					foreach (var n in notifyPositionsUpdated)
						n.UpdatedBaseCenter(self.Location);
				}
			}
		}
	}
}
