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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants experience to this actor when enabled.")]
	public class GainsExperienceOnConditionInfo : ConditionalTraitInfo
	{
		[Desc("Flat amount of experience to grant.")]
		public readonly int Experience = 0;

		[Desc("Additional experience to grant as a percentage of this actor's value.")]
		public readonly int ValuePercentageExperience = 0;

		[Desc("Percentage modifier to apply to the experience granted to this actor.")]
		public readonly int ActorExperienceModifier = 10000;

		[Desc("If true, experience is granted each time this trait is enabled.")]
		public readonly bool Repeatable = false;

		public override object Create(ActorInitializer init) { return new GainsExperienceOnCondition(this); }
	}

	public class GainsExperienceOnCondition : ConditionalTrait<GainsExperienceOnConditionInfo>
	{
		bool experienceGranted;

		public GainsExperienceOnCondition(GainsExperienceOnConditionInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			if (!Info.Repeatable && experienceGranted)
				return;

			var gainsExperience = self.TraitOrDefault<GainsExperience>();
			if (gainsExperience == null)
				return;

			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			var valueExperience = valued != null
				? Util.ApplyPercentageModifiers(valued.Cost, new[] { Info.ValuePercentageExperience })
				: 0;
			var experience = Info.Experience + valueExperience;
			var experienceModifiers = self.TraitsImplementing<IGainsExperienceModifier>()
				.Select(m => m.GetGainsExperienceModifier())
				.Append(Info.ActorExperienceModifier);

			gainsExperience.GiveExperience(Util.ApplyPercentageModifiers(experience, experienceModifiers));
			experienceGranted = true;
		}
	}
}