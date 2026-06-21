#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Tracks independent stack cooldowns for stackable support powers.")]
	public class StackableSupportPowerManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new StackableSupportPowerManager(); }
	}

	public sealed class StackableSupportPowerDefinition
	{
		public readonly int ChargeInterval;
		public readonly int StackActivationInterval;
		public readonly int MaxStacks;
		public readonly bool OneShot;
		public readonly bool StartFullyCharged;

		public StackableSupportPowerDefinition(int chargeInterval, int stackActivationInterval, int maxStacks, bool oneShot, bool startFullyCharged)
		{
			ChargeInterval = chargeInterval;
			StackActivationInterval = stackActivationInterval;
			MaxStacks = maxStacks;
			OneShot = oneShot;
			StartFullyCharged = startFullyCharged;
		}
	}

	public sealed class StackableSupportPowerStack
	{
		public readonly string Id;
		public int RemainingSubTicks;

		public StackableSupportPowerStack(string id, int remainingSubTicks)
		{
			Id = id;
			RemainingSubTicks = remainingSubTicks;
		}
	}

	sealed class StackableSupportPowerState
	{
		public StackableSupportPowerDefinition Definition;
		public readonly Dictionary<string, StackableSupportPowerStack> Stacks = new Dictionary<string, StackableSupportPowerStack>(StringComparer.Ordinal);
		public readonly HashSet<string> ConsumedSources = new HashSet<string>(StringComparer.Ordinal);
		public int ActivationRemainingSubTicks;
	}

	public class StackableSupportPowerManager
	{
		readonly Dictionary<string, StackableSupportPowerState> powers = new Dictionary<string, StackableSupportPowerState>(StringComparer.Ordinal);

		public void RegisterPower(string orderName, int chargeInterval, int stackActivationInterval, int maxStacks, bool oneShot, bool startFullyCharged)
		{
			var definition = new StackableSupportPowerDefinition(chargeInterval, stackActivationInterval, maxStacks, oneShot, startFullyCharged);
			if (!powers.TryGetValue(orderName, out var state))
			{
				state = new StackableSupportPowerState();
				powers.Add(orderName, state);
			}

			state.Definition = definition;
		}

		public bool AddStack(string orderName, string sourceId)
		{
			if (!powers.TryGetValue(orderName, out var state) || state.Definition == null)
				return false;

			if (state.ConsumedSources.Contains(sourceId) || state.Stacks.ContainsKey(sourceId))
				return false;

			if (state.Definition.MaxStacks > 0 && state.Stacks.Count >= state.Definition.MaxStacks)
				return false;

			var remainingSubTicks = state.Definition.StartFullyCharged ? 0 : state.Definition.ChargeInterval * 100;
			state.Stacks.Add(sourceId, new StackableSupportPowerStack(sourceId, remainingSubTicks));
			return true;
		}

		public void RemoveStack(string orderName, string sourceId)
		{
			if (!powers.TryGetValue(orderName, out var state))
				return;

			state.Stacks.Remove(sourceId);

			// if no stacks remain, reset the activation timer to allow the next stack to be used immediately
			if (state.Stacks.Count == 0)
				state.ActivationRemainingSubTicks = 0;
		}

		public void Tick(string orderName)
		{
			if (!powers.TryGetValue(orderName, out var state) || state.Definition == null)
				return;

			if (state.ActivationRemainingSubTicks > 0)
				state.ActivationRemainingSubTicks = Math.Max(0, state.ActivationRemainingSubTicks - 100);

			foreach (var stack in state.Stacks.Values)
			{
				if (stack.RemainingSubTicks > 0)
					stack.RemainingSubTicks = Math.Max(0, stack.RemainingSubTicks - 100);
			}
		}

		public bool HasStacks(string orderName)
		{
			return powers.TryGetValue(orderName, out var state) && state.Stacks.Count > 0;
		}

		public int GetTotalStackCount(string orderName)
		{
			return powers.TryGetValue(orderName, out var state) ? state.Stacks.Count : 0;
		}

		public int GetReadyStackCount(string orderName)
		{
			return powers.TryGetValue(orderName, out var state)
				? state.Stacks.Values.Count(s => s.RemainingSubTicks == 0) : 0;
		}

		public int GetActivationRemainingTicks(string orderName)
		{
			return powers.TryGetValue(orderName, out var state) ? state.ActivationRemainingSubTicks / 100 : 0;
		}

		public int GetDisplayRemainingSubTicks(string orderName)
		{
			if (!powers.TryGetValue(orderName, out var state) || state.Definition == null)
				return 0;

			if (state.Stacks.Count == 0)
				return state.Definition.ChargeInterval * 100;

			var shortestStackRemaining = state.Stacks.Values.Min(s => s.RemainingSubTicks);
			return Math.Max(shortestStackRemaining, state.ActivationRemainingSubTicks);
		}

		public bool CanActivate(string orderName)
		{
			return powers.TryGetValue(orderName, out var state) && state.ActivationRemainingSubTicks == 0
				&& state.Stacks.Values.Any(s => s.RemainingSubTicks == 0);
		}

		public bool TryActivate(string orderName)
		{
			if (!powers.TryGetValue(orderName, out var state) || state.Definition == null || state.ActivationRemainingSubTicks > 0)
				return false;

			var stack = state.Stacks.Values
				.Where(s => s.RemainingSubTicks == 0)
				.OrderBy(s => s.Id, StringComparer.Ordinal)
				.FirstOrDefault();

			if (stack == null)
				return false;

			state.ActivationRemainingSubTicks = state.Definition.StackActivationInterval * 100;

			if (state.Definition.OneShot)
			{
				state.Stacks.Remove(stack.Id);
				state.ConsumedSources.Add(stack.Id);
			}
			else
				stack.RemainingSubTicks = state.Definition.ChargeInterval * 100;

			return true;
		}
	}
}
