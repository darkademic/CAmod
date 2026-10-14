#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Automatically orders idle harvesters to resume harvesting after a configurable delay.")]
	class HarvesterAutoResumeInfo : ConditionalTraitInfo
	{
		[Desc("Delay in ticks before an idle harvester will automatically resume harvesting.")]
		public readonly int ResumeDelay = 250;

		public override object Create(ActorInitializer init) { return new HarvesterAutoResume(this); }
	}

	class HarvesterAutoResume : ConditionalTrait<HarvesterAutoResumeInfo>, INotifyIdle
	{
		int idleTicks;

		public HarvesterAutoResume(HarvesterAutoResumeInfo info)
			: base(info) { }

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (++idleTicks < Info.ResumeDelay)
				return;

			idleTicks = 0;
			self.QueueActivity(new FindAndDeliverResources(self));
		}
	}
}
