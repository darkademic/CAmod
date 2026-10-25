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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lets the actor generate cash in a set periodic time and tracks income by type.")]
	public class CashTricklerCAInfo : PausableConditionalTraitInfo, IRulesetLoaded
	{
		[Desc("Number of ticks to wait between giving money.")]
		public readonly int Interval = 50;

		[Desc("Number of ticks to wait before giving first money.")]
		public readonly int InitialDelay = 0;

		[Desc("Amount of money to give each time.")]
		public readonly int Amount = 15;

		[Desc("Income category credited to the owner.")]
		public readonly PlayerIncomeType Type = PlayerIncomeType.Oil;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long to show the cash tick indicator when enabled.")]
		public readonly int DisplayDuration = 30;

		[Desc("Use resource storage for cash granted.")]
		public readonly bool UseResourceStorage = false;

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (ShowTicks && !info.HasTraitInfo<IOccupySpaceInfo>())
				throw new YamlException($"CashTricklerCA is defined with ShowTicks 'true' but actor '{info.Name}' occupies no space.");
		}

		public override object Create(ActorInitializer init) { return new CashTricklerCA(this); }
	}

	public class CashTricklerCA : PausableConditionalTrait<CashTricklerCAInfo>, ITick, ISync, INotifyCreated, INotifyOwnerChanged
	{
		readonly CashTricklerCAInfo info;
		PlayerResources resources;
		PlayerIncome income;

		[Sync]
		public int Ticks { get; private set; }

		public CashTricklerCA(CashTricklerCAInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.InitialDelay;
		}

		protected override void Created(Actor self)
		{
			UpdatePlayerTraits(self.Owner);
			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			UpdatePlayerTraits(newOwner);
		}

		void UpdatePlayerTraits(Player owner)
		{
			resources = owner.PlayerActor.Trait<PlayerResources>();
			income = owner.PlayerActor.Trait<PlayerIncome>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				Ticks = info.Interval;

			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--Ticks < 0)
			{
				var modifiers = self.TraitsImplementing<ICashTricklerModifier>().Select(x => x.GetCashTricklerModifier());
				Ticks = info.Interval;
				var amount = Util.ApplyPercentageModifiers(info.Amount, modifiers);
				amount = ModifyCash(self, amount);
				income.AddIncome(info.Type, amount);
			}
		}

		int ModifyCash(Actor self, int amount)
		{
			if (info.UseResourceStorage)
			{
				var initialAmount = resources.Resources;
				resources.GiveResources(amount);
				amount = resources.Resources - initialAmount;
			}
			else
				amount = resources.ChangeCash(amount);

			if (info.ShowTicks && amount != 0)
				self.World.AddFrameEndTask(w => w.Add(
					new FloatingText(self.CenterPosition, self.OwnerColor(), FloatingText.FormatCashTick(amount), info.DisplayDuration)));

			return amount;
		}
	}
}
