#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.CA.Scripting
{
	[ScriptGlobal("BeaconCA")]
	public class BeaconCAGlobal : ScriptGlobal
	{
		readonly RadarPings radarPings;

		public BeaconCAGlobal(ScriptContext context)
			: base(context)
		{
			radarPings = context.World.WorldActor.TraitOrDefault<RadarPings>();
		}

		[Desc("Creates a new beacon that stays for the specified time at the specified WPos. " +
			"Does not remove player set beacons, nor gets removed by placing them. " +
			"Requires the 'PlaceBeacon' trait on the player actor.")]
		public void New(Player owner, WPos position, int duration = 750, bool showRadarPings = true,
			string beaconPoster = null, string beaconPosterPalette = "chrome")
		{
			var beacon = owner.PlayerActor.Info.TraitInfoOrDefault<PlaceBeaconInfo>();
			if (beacon == null)
				throw new LuaException("The player actor has no 'PlaceBeacon' trait.");

			var playerBeacon = new Beacon(owner, position, beacon.IsPlayerPalette, beacon.Palette,
				beacon.BeaconImage, beaconPoster, beaconPosterPalette, beacon.BeaconSequence,
				beacon.ArrowSequence, beacon.CircleSequence, null, null, duration: duration);

			owner.PlayerActor.World.AddFrameEndTask(w => w.Add(playerBeacon));

			if (showRadarPings && radarPings != null)
			{
				radarPings.Add(
					() => owner.IsAlliedWith(owner.World.RenderPlayer),
					position,
					owner.Color,
					duration);
			}
		}
	}
}
