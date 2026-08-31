#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class GrantChargingConditionProperties : ScriptActorProperties, Requires<GrantChargingConditionInfo>
	{
		readonly GrantChargingCondition trait;

		public GrantChargingConditionProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			trait = self.TraitsImplementing<GrantChargingCondition>().FirstOrDefault();
		}

		[Desc("Gets the current charge percentage of the actor's charging condition.")]
		public int ChargePercentage => trait.ChargePercentage;
	}
}
