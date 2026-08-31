#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class GrantConditionOnDeployProperties : ScriptActorProperties, Requires<GrantConditionOnDeployInfo>
	{
		readonly GrantConditionOnDeploy trait;

		public GrantConditionOnDeployProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			trait = self.Trait<GrantConditionOnDeploy>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Queue deploy.")]
		public void Deploy()
		{
			trait.Deploy();
		}

		[ScriptActorPropertyActivity]
		[Desc("Queue undeploy.")]
		public void Undeploy()
		{
			trait.Undeploy();
		}
	}
}
