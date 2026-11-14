#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Modifies the cash given by cash tricker traits of this actor.")]
	public class CashTricklerCAMultiplierInfo : ConditionalTraitInfo, Requires<CashTricklerCAInfo>
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new CashTricklerCAMultiplier(this); }
	}

	public class CashTricklerCAMultiplier : ConditionalTrait<CashTricklerCAMultiplierInfo>, ICashTricklerModifier
	{
		public CashTricklerCAMultiplier(CashTricklerCAMultiplierInfo info)
			: base(info) { }

		int ICashTricklerModifier.GetCashTricklerModifier()
		{
			return IsTraitDisabled ? 100 : Info.Modifier;
		}
	}
}
