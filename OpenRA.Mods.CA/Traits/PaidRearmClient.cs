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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Charges the rearming actor for ammo restored at this resupplier. Ammo is removed again if payment fails.")]
	public class PaidRearmClientInfo : TraitInfo
	{
		[Desc("Cash cost per ammo point restored.")]
		public readonly int CostPerAmmo = 0;

		[Desc("Host actor types that charge for rearming this actor.")]
		[ActorReference]
		public readonly HashSet<string> ValidHostTypes = new();

		public override object Create(ActorInitializer init) { return new PaidRearmClient(this); }
	}

	public class PaidRearmClient
	{
		public readonly PaidRearmClientInfo Info;

		public PaidRearmClient(PaidRearmClientInfo info)
		{
			Info = info;
		}
	}
}
