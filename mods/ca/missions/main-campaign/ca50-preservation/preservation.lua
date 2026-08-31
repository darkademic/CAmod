MissionDir = "ca|missions/main-campaign/ca50-preservation"

ScrinAttackPaths = {
	{ ScrinWaypoint1.Location, ScrinWaypoint2.Location },
    { ScrinWaypoint3.Location, ScrinWaypoint4.Location },
    { ScrinWaypoint5.Location, ScrinWaypoint6.Location },
    { ScrinWaypoint5.Location, ScrinWaypoint6.Location, ScrinWaypoint7.Location },
    { ScrinWaypoint5.Location, ScrinWaypoint8.Location },
}

SovietAttackPaths = {
	{ SovietWaypoint1.Location, SovietWaypoint2.Location },
    { SovietWaypoint1.Location, SovietWaypoint2.Location },
    { SovietWaypoint4.Location, SovietWaypoint5.Location },
}

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 25) + 17),
	vhard = DateTime.Seconds((60 * 20) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
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
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
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
    Nod = Player.GetPlayer("Nod")
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

    ObjectiveProtectTemples = ScrinRebels.AddObjective("Protect the Nod Temples until gateways are operational.")

    UpdateGatewayStatus()

    Trigger.OnAnyKilled({ WestTemple, MiddleTemple, EastTemple }, function(self)
        if not ScrinRebels.IsObjectiveCompleted(ObjectiveProtectTemples) then
            ScrinRebels.MarkFailedObjective(ObjectiveProtectTemples)
        end
    end)

    local initialAttackWaves = Utils.Shuffle({ SovietInitialAttack1, SovietInitialAttack2, SovietInitialAttack3, ScrinInitialAttack1, ScrinInitialAttack2, ScrinInitialAttack3 })
    local initialAttackDelay = 0

    Utils.Do(initialAttackWaves, function(w)
        Trigger.AfterDelay(initialAttackDelay, function()
            local units = Map.ActorsInCircle(w.CenterPosition, WDist.New(9 * 1024), function(a)
                return (a.Owner == USSR or a.Owner == Scrin) and a.HasProperty("Hunt")
            end)
            Utils.Do(units, function(u)
                u.Hunt()
            end)
        end)
        initialAttackDelay = initialAttackDelay + DateTime.Seconds(20)
    end)

    Trigger.AfterDelay(DateTime.Seconds(5), function()
        Tip("Use the Charge Gateway power to use resources to charge the three gateways.")
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

        UpdateGatewayStatus()

		if Scrin.HasNoRequiredUnits() and USSR.HasNoRequiredUnits() then
            ScrinRebels.MarkCompletedObjective(ObjectiveProtectTemples)
		end

		if MissionPlayersHaveNoRequiredUnits() then
            if not ScrinRebels.IsObjectiveCompleted(ObjectiveProtectTemples) then
                ScrinRebels.MarkFailedObjective(ObjectiveProtectTemples)
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

    InitScrinAttacks()
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

    InitUSSRAttacks()
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

UpdateGatewayStatus = function()
    local westCharge = 100
    if not WestGateway.IsDead then
        westCharge = WestGateway.ChargePercentage
    end

    local middleCharge = 100
    if not MiddleGateway.IsDead then
        middleCharge = MiddleGateway.ChargePercentage
    end

    local eastCharge = 100
    if not EastGateway.IsDead then
        eastCharge = EastGateway.ChargePercentage
    end

    UserInterface.SetMissionText("Gateway progress: Western: " .. westCharge .. "% -- Central: " .. middleCharge .. "% -- Eastern: " .. eastCharge .. "%", HSLColor.Yellow)

	if not WestGatewayCharged and westCharge == 100 then
		WestGatewayCharged = true
		Notification("Western gateway fully charged.")
		MediaCA.PlaySound(MissionDir .. "/s_westcharged.aud", 2)
	end

	if not MiddleGatewayCharged and middleCharge == 100 then
		MiddleGatewayCharged = true
		Notification("Central gateway fully charged.")
		MediaCA.PlaySound(MissionDir .. "/s_centralcharged.aud", 2)
	end
	if not EastGatewayCharged and eastCharge == 100 then
		EastGatewayCharged = true
		Notification("Eastern gateway fully charged.")
		MediaCA.PlaySound(MissionDir .. "/s_eastcharged.aud", 2)
	end

    if westCharge == 100 and middleCharge == 100 and eastCharge == 100 then
        ScrinRebels.MarkCompletedObjective(ObjectiveProtectTemples)
    end
end
