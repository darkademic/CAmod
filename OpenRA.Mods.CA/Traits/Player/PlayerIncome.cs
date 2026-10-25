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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum PlayerIncomeType
	{
		Oil
	}

	[TraitLocation(SystemActors.Player)]
	public class PlayerIncomeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new PlayerIncome(); }
	}

	public class PlayerIncome : ISync
	{
		[Sync]
		int oilIncome;

		public int GetIncome(PlayerIncomeType type)
		{
			return type switch
			{
				PlayerIncomeType.Oil => oilIncome,
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		}

		public void AddIncome(PlayerIncomeType type, int amount)
		{
			switch (type)
			{
				case PlayerIncomeType.Oil:
					oilIncome += amount;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}
	}
}
