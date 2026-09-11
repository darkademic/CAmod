MissionDir = "ca|missions/main-campaign/ca49-extraction"

RebelStructures = {
    RebelStructures1,
    RebelStructures2,
    RebelStructures3,
    RebelStructures4,
    RebelStructures5,
    RebelStructures6,
    RebelStructures7,
    RebelStructures8,
    RebelStructures9,
    RebelStructures10,
    RebelStructures11
}

NodStrandedUnits = {
    NodStrandedUnits1,
    NodStrandedUnits2,
    NodStrandedUnits3,
    NodStrandedUnits4,
	NodStrandedUnits5
}

DeadlineTime = {
	easy = DateTime.Minutes(20),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(10),
	vhard = DateTime.Minutes(8),
	brutal = DateTime.Minutes(6)
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 25) + 17),
	vhard = DateTime.Seconds((60 * 20) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
}

ScrinAttackPaths = {
	{ ScrinWaypoint1.Location, ScrinWaypoint3.Location, NodBaseCenter.Location },
    { ScrinWaypoint2.Location, ScrinWaypoint4.Location, NodBaseCenter.Location },
    { ScrinWaypoint1.Location, ScrinWaypoint3.Location, ScrinWaypoint5.Location },
}

SovietAttackPaths = {
    { SovietWaypoint1.Location, SovietWaypoint3.Location, SovietWaypoint5.Location, NodBaseCenter.Location },
    { SovietWaypoint2.Location, SovietWaypoint4.Location, SovietWaypoint6.Location, NodBaseCenter.Location },
    { SovietWaypoint2.Location, SovietWaypoint4.Location, SovietWaypoint7.Location, NodBaseCenter.Location },
    { SovietWaypoint1.Location, SovietWaypoint3.Location, SovietWaypoint6.Location, SovietWaypoint8.Location },
}

Squads = {
	ScrinMain = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Scrin),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		AttackPaths = ScrinAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
	},
	SovietMain = {
		Compositions = AdjustCompositionsForDifficulty(UnitCompositions.Soviet),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40, RampDuration = DateTime.Minutes(15) }),
		FollowLeader = true,
		AttackPaths = SovietAttackPaths,
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(3)),
	},
	ScrinAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Scrin,
	},
	SovietAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Soviet,
	}
}

SetupPlayers = function()
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
    ScrinRebelsInactive = Player.GetPlayer("ScrinRebelsInactive")
    Nod = Player.GetPlayer("Nod")
	NodInactive = Player.GetPlayer("NodInactive")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { ScrinRebels }
	MissionEnemies = { USSR, Scrin }

	Actor.Create("rebel.allegiance", true, { Owner = ScrinRebels })
end

WorldLoaded = function()
	SetupPlayers()

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(ScrinRebels)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitUSSR()
	InitScrin()

	if IsNormalOrBelow() then
		Utils.Do(MissionPlayers, function(p)
			Actor.Create("mcv.allowed", true, { Owner = p })
		end)
	end

    ObjectivePrepare = ScrinRebels.AddObjective("Gather forces and rendezvous with Kane before deadline.")
    ObjectiveDestroyEitherBase = ScrinRebels.AddObjective("Eliminate either Scrin or Soviet base.")
    ObjectiveProtectTemple = ScrinRebels.AddObjective("Kane's Temple must survive.")

    TimerTicks = DeadlineTime[Difficulty]
    UpdateDeadlineCountdown()

    Trigger.OnEnteredProximityTrigger(KaneLocator.CenterPosition, WDist.New(12 * 1024), function(a, id)
        if IsMissionPlayer(a.Owner) and a.HasProperty("Move") and not ScrinRebels.IsObjectiveCompleted(ObjectivePrepare) then
            Trigger.RemoveProximityTrigger(id)
            RendezvousComplete()
        end
    end)

    Utils.Do(NodStrandedUnits, function(w)
        Trigger.OnEnteredProximityTrigger(w.CenterPosition, WDist.New(6 * 1024), function(a, id)
            if IsMissionPlayer(a.Owner) and a.HasProperty("Move") then
                Trigger.RemoveProximityTrigger(id)
                local nodUnits = Map.ActorsInCircle(w.CenterPosition, WDist.New(6 * 1024), function(a)
                    return a.Owner == NodInactive
                end)
				Notification("Nod units located.")
				MediaCA.PlaySound(MissionDir .. "/s_nodunitslocated.aud", 2)
                TransferStrandedNodUnits(nodUnits)
            end
        end)
    end)

    Utils.Do(RebelStructures, function(w)
        Trigger.OnEnteredProximityTrigger(w.CenterPosition, WDist.New(11 * 1024), function(a, id)
            if IsMissionPlayer(a.Owner) and a.HasProperty("Move") then
                Trigger.RemoveProximityTrigger(id)
                local rebelStructures = Map.ActorsInCircle(w.CenterPosition, WDist.New(11 * 1024), function(a)
                    return a.Owner == ScrinRebelsInactive
                end)
				Notification("Rebel structures reclaimed.")
				MediaCA.PlaySound(MissionDir .. "/s_rebstrucreclaimed.aud", 2)
                TransferRebelStructures(rebelStructures)
            end
        end)
    end)

	Trigger.OnKilled(KanesTemple, function(self, killer)
		if not ScrinRebels.IsObjectiveCompleted(ObjectiveProtectTemple) then
			ScrinRebels.MarkFailedObjective(ObjectiveProtectTemple)
		end
	end)

	local harvs = Utils.Where(ScrinRebelsInactive.GetActors(), function(a) return a.Type == "harv.scrin" end)
	Utils.Do(harvs, function(h)
		h.Stop()
	end)

    AfterWorldLoaded()
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
	OncePerThirtySecondChecks()
	AfterTick()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500
        USSR.Resources = USSR.ResourceCapacity - 500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
			UpdateDeadlineCountdown()
		end

		if not PlayerHasBuildings(Scrin) or not PlayerHasBuildings(USSR) then
			ScrinRebels.MarkCompletedObjective(ObjectiveDestroyEitherBase)
			ScrinRebels.MarkCompletedObjective(ObjectiveProtectTemple)
		end

		if MissionPlayersHaveNoRequiredUnits() then
			if ObjectivePrepare ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectivePrepare) then
				ScrinRebels.MarkFailedObjective(ObjectivePrepare)
			end
			if ObjectiveDestroyEitherBase ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectiveDestroyEitherBase) then
				ScrinRebels.MarkFailedObjective(ObjectiveDestroyEitherBase)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocations()
	end
end

OncePerThirtySecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 750 == 0 then
		CalculatePlayerCharacteristics()
	end
end

UpdateDeadlineCountdown = function()
	if not IsKaneReached and ObjectivePrepare ~= nil and not ScrinRebels.IsObjectiveCompleted(ObjectivePrepare) then
		UserInterface.SetMissionText("Rendezvous with Kane before the deadline: " .. UtilsCA.FormatTimeForGameSpeed(TimerTicks), HSLColor.Yellow)
        if TimerTicks <= 0 then
            ScrinRebels.MarkFailedObjective(ObjectivePrepare)
        end
	end
end

RendezvousComplete = function()
    ScrinRebels.MarkCompletedObjective(ObjectivePrepare)
    UserInterface.SetMissionText("")

    local nodBaseStructures = Map.ActorsInCircle(NodBaseCenter.CenterPosition, WDist.FromCells(15), function(a)
        return a.Owner == NodInactive and not a.IsDead and not a.HasProperty("AttackMove") and a.Type ~= "msg"
    end)

	TransferNodBaseStructures(nodBaseStructures)


    local nodBaseUnits = Map.ActorsInCircle(KaneLocator.CenterPosition, WDist.FromCells(13), function(a)
        return a.Owner == NodInactive and not a.IsDead and (a.HasProperty("AttackMove") or a.Type == "msg")
    end)

	TransferNodBaseUnits(nodBaseUnits)

    KaneLocator.Destroy()

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.DisplayMessage("Not a moment too soon, supervisor. Our enemies are closing in. We must break through the defenses to the south.", "Kane", HSLColor.FromHex("FF0000"))
		MediaCA.PlaySound(MissionDir .. "/kane_notamoment.aud", 2)
	end)

    InitScrinAttacks()
    InitUSSRAttacks()
end

TransferNodBaseUnits = function(units)
    Utils.Do(units, function(a)
        a.Owner = ScrinRebels
		if a.Type == "msg" then
			a.Undeploy()
		end
    end)
end

TransferNodBaseStructures = function(structures)
	Utils.Do(structures, function(a)
		a.Owner = Nod
	end)
	AutoRepairBuildings(Nod)
end

TransferStrandedNodUnits = function(units)
    Utils.Do(units, function(a)
        a.Owner = ScrinRebels
		if a.Type == "msg" then
			a.Undeploy()
		end
    end)
end

TransferRebelStructures = function(structures)
    Utils.Do(structures, function(a)
        a.Owner = ScrinRebels
    end)
	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = ScrinRebels })
	end)
end

InitScrin = function()
	AutoRepairAndRebuildBuildings(Scrin)
	SetupRefAndSilosCaptureCredits(Scrin)
	AutoReplaceHarvesters(Scrin)
	AutoRebuildConyards(Scrin)

	local scrinGroundAttackers = Scrin.GetGroundAttackers()
	Utils.Do(scrinGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsScrinGroundHunterUnit)
	end)
end

InitScrinAttacks = function()
	InitAiUpgrades(Scrin)
	InitAttackSquad(Squads.ScrinMain, Scrin)
	InitAirAttackSquad(Squads.ScrinAir, Scrin)

    Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = Scrin })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = Scrin })
	end)
end

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	AutoRebuildConyards(USSR)

	local ussrGroundAttackers = USSR.GetGroundAttackers()
	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsUSSRGroundHunterUnit)
	end)
end

InitUSSRAttacks = function()
	InitAiUpgrades(USSR)
	InitAttackSquad(Squads.SovietMain, USSR)
	InitAirAttackSquad(Squads.SovietAir, USSR)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = USSR })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = USSR })
	end)
end
