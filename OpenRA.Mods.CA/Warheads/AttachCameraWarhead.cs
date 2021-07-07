﻿#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("This warhead can attach a camera to the target.")]
	public class AttachCameraWarhead : WarheadAS
	{
		[ActorReference(typeof(AttachableCameraInfo))]
		[FieldLoader.Require]
		[Desc("Camera actor.")]
		public readonly string CameraActor = null;

		[Desc("Range of targets to be attached.")]
		public readonly WDist Range = WDist.FromCells(1);

		[Desc("Maximum number of targets to attach cameras to.")]
		public readonly int MaxTargets = 1;

		[Desc("List of sounds that can be played on attaching.")]
		public readonly string[] AttachSounds = new string[0];

		[Desc("List of sounds that can be played if attaching is not possible.")]
		public readonly string[] MissSounds = new string[0];

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;

			if (!IsValidImpact(pos, firedBy))
				return;

			var world = firedBy.World;
			var availableActors = firedBy.World.FindActorsOnCircle(pos, Range);
			var numAttached = 0;

			foreach (var actor in availableActors)
			{
				if (!IsValidAgainst(actor, firedBy))
					continue;

				if (actor.IsDead)
					continue;

				var activeShapes = actor.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any())
					continue;

				var distance = activeShapes.Min(t => t.DistanceFromEdge(actor, pos));

				if (distance > Range)
					continue;

				var targetTrait = actor.TraitsImplementing<AttachableCameraTarget>().FirstOrDefault();

				if (targetTrait != null)
				{
					AttachCamera(actor, firedBy, targetTrait);
					numAttached++;

					var attachSound = AttachSounds.RandomOrDefault(world.LocalRandom);
					if (attachSound != null)
						Game.Sound.Play(SoundType.World, attachSound, pos);
				}
				else
				{
					var failSound = MissSounds.RandomOrDefault(world.LocalRandom);
					if (failSound != null)
						Game.Sound.Play(SoundType.World, failSound, pos);
				}

				if (numAttached > MaxTargets)
					break;
			}
		}

		void AttachCamera(Actor targetActor, Actor firedBy, AttachableCameraTarget targetTrait)
		{
			var map = targetActor.World.Map;
			var targetCell = map.CellContaining(targetActor.CenterPosition);

			targetActor.World.AddFrameEndTask(w =>
			{
				var cameraActor = targetActor.World.CreateActor(CameraActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(targetCell),
					new OwnerInit(firedBy.Owner),
				});

				targetTrait.AttachCamera(cameraActor);
			});
		}
	}
}
