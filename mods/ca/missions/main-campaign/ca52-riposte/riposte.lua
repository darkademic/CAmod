MissionDir = "ca|missions/main-campaign/ca52-riposte"

SuperweaponsEnabledTime = {
	easy = DateTime.Seconds((60 * 50) + 17),
	normal = DateTime.Seconds((60 * 35) + 17),
	hard = DateTime.Seconds((60 * 25) + 17),
	vhard = DateTime.Seconds((60 * 20) + 17),
	brutal = DateTime.Seconds((60 * 15) + 17)
}

AdjustedGDICompositions = AdjustCompositionsForDifficulty(UnitCompositions.GDI)
AdjustedNodCompositions = AdjustCompositionsForDifficulty(UnitCompositions.Nod)

Squads = {
	GDIMain1 = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(4)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		Compositions = AdjustedGDICompositions,
		ProducerActors = { Infantry = { HawthorneWestBarracks1, HawthorneWestBarracks2 }, Vehicles = { HawthorneWestFactory1, HawthorneWestFactory2 } },
		AttackPaths = {
			{ HawthorneWaypoint1.Location, HawthorneWaypoint6.Location, HawthorneWaypoint8.Location },
		},
	},
	GDIMain2 = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(6)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 20, Max = 40 }),
		FollowLeader = true,
		Compositions = AdjustedGDICompositions,
		ProducerActors = { Infantry = { HawthorneEastBarracks1, HawthorneEastBarracks2 }, Vehicles = { HawthorneEastFactory1, HawthorneEastFactory2 } },
		AttackPaths = {
			{ HawthorneWaypoint7.Location, HawthorneWaypoint8.Location },
			{ HawthorneWaypoint3.Location, HawthorneWaypoint8.Location },
			{ HawthorneWaypoint5.Location },
		},
	},
	GDIVsNod1 = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		Compositions = AdjustedGDICompositions,
		ProducerActors = { Infantry = { HawthorneWestBarracks1, HawthorneWestBarracks2 }, Vehicles = { HawthorneWestFactory1, HawthorneWestFactory2 } },
		AttackPaths = {
			{ HawthorneWaypoint2.Location },
			{ HawthorneWaypoint2.Location, HawthorneWaypoint9.Location },
		},
	},
	GDIVsNod2 = {
		Delay = AdjustDelayForDifficulty(DateTime.Minutes(1)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 10, Max = 20 }),
		FollowLeader = true,
		Compositions = AdjustedGDICompositions,
		ProducerActors = { Infantry = { HawthorneEastBarracks1, HawthorneEastBarracks2 }, Vehicles = { HawthorneEastFactory1, HawthorneEastFactory2 } },
		AttackPaths = {
			{ HawthorneWaypoint4.Location },
			{ HawthorneWaypoint3.Location, HawthorneWaypoint9.Location },
		},
	},
	GDIAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.GDI,
	},
	AntiHeavyAir = AntiHeavyAirSquad({ "orcb" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	AirToAir = AirToAirSquad({ "orca" }, AdjustAirDelayForDifficulty(DateTime.Minutes(10))),
	Nod = {
		Delay = DateTime.Minutes(2),
		AttackValuePerSecond = { Min = 15, Max = 30 },
		DispatchDelay = DateTime.Seconds(15),
		FollowLeader = true,
		Compositions = AdjustedNodCompositions,
		AttackPaths = {
			{ NodWaypoint1.Location },
			{ NodWaypoint2.Location },
		},
	},
	NodAir = {
		Delay = AdjustAirDelayForDifficulty(DateTime.Minutes(13)),
		AttackValuePerSecond = AdjustAttackValuesForDifficulty({ Min = 12, Max = 12 }),
		Compositions = AirCompositions.Nod,
	},
}

SetupPlayers = function()
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	HawthorneGDI = Player.GetPlayer("HawthorneGDI")
    Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = { ScrinRebels }
	MissionEnemies = { HawthorneGDI }

	Actor.Create("rebel.allegiance", true, { Owner = ScrinRebels })
end

WorldLoaded = function()
	SetupPlayers()

    Camera.Position = PlayerStart.CenterPosition

	InitObjectives(ScrinRebels)
	AdjustPlayerStartingCashForDifficulty()
	RemoveActorsBasedOnDifficultyTags()
	InitHawthorneGDI()
	InitNod()

    ObjectiveEliminateHawthorne = ScrinRebels.AddObjective("Eliminate Hawthorne's forces.")
	ObjectiveProtectTemple = ScrinRebels.AddObjective("Nod Temple Prime must survive.")

    Trigger.OnKilled(TemplePrime, function(self, killer)
        if not ScrinRebels.IsObjectiveCompleted(ObjectiveProtectTemple) then
            ScrinRebels.MarkFailedObjective(ObjectiveProtectTemple)
        end
    end)

	Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(3)), function()
		Media.DisplayMessage("It's the end of the road for you, Kane! It's time to pay for all the suffering you've caused.", "Gen. Hawthorne", HSLColor.FromHex("FF6600"))
		MediaCA.PlaySound(MissionDir .. "/hth_endofroad.aud", 2)
		Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(6)), function()
			Media.DisplayMessage("You have me at a disadvantage General. Suffering is inevitable in any fight for freedom, whereas you side with tyrants who seek to destroy our last hope for unity. They will fail. As will you.", "Kane", HSLColor.FromHex("FF0000"))
			MediaCA.PlaySound(MissionDir .. "/kane_suffering.aud", 2)
			Trigger.AfterDelay(AdjustTimeForGameSpeed(DateTime.Seconds(14)), function()
				Media.DisplayMessage("Maybe you've started to believe your own propaganda, but I don't care. You're either evil, or delusional, and I'm doing what needs to be done to deliver justice.", "Gen. Hawthorne", HSLColor.FromHex("FF6600"))
				MediaCA.PlaySound(MissionDir .. "/hth_justice.aud", 2)
			end)
		end)
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
		HawthorneGDI.Resources = HawthorneGDI.ResourceCapacity - 500

		if not PlayerHasBuildings(HawthorneGDI) then
			ScrinRebels.MarkCompletedObjective(ObjectiveEliminateHawthorne)
		end

		if MissionPlayersHaveNoRequiredUnits() then
			ScrinRebels.MarkFailedObjective(ObjectiveEliminateHawthorne)
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

InitNod = function()
	AutoRepairAndRebuildBuildings(Nod, 15)
	SetupRefAndSilosCaptureCredits(Nod)
	AutoReplaceHarvesters(Nod)

	local nodGroundAttackers = Nod.GetGroundAttackers()

	Utils.Do(nodGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsNodGroundHunterUnit)
	end)

	InitAttackSquad(Squads.Nod, Nod, HawthorneGDI)
	InitAirAttackSquad(Squads.NodAir, Nod, HawthorneGDI)
end

InitHawthorneGDI = function()
	AutoRepairAndRebuildBuildings(HawthorneGDI, 15)
	SetupRefAndSilosCaptureCredits(HawthorneGDI)
	AutoReplaceHarvesters(HawthorneGDI)
	AutoRebuildConyards(HawthorneGDI)
	InitAiUpgrades(HawthorneGDI)

	local gdiGroundAttackers = HawthorneGDI.GetGroundAttackers()

	Utils.Do(gdiGroundAttackers, function(a)
		TargetSwapChance(a, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGDIGroundHunterUnit)
	end)

	Trigger.AfterDelay(SuperweaponsEnabledTime[Difficulty], function()
		Actor.Create("ai.superweapons.enabled", true, { Owner = HawthorneGDI })
		Actor.Create("ai.minor.superweapons.enabled", true, { Owner = HawthorneGDI })
	end)

	InitHawthorneGDIAttacks()
end

InitHawthorneGDIAttacks = function()
	InitAttackSquad(Squads.GDIMain1, HawthorneGDI)
	InitAttackSquad(Squads.GDIMain2, HawthorneGDI)
	InitAttackSquad(Squads.GDIVsNod1, HawthorneGDI, Nod)
	InitAttackSquad(Squads.GDIVsNod2, HawthorneGDI, Nod)
	InitAirAttackSquad(Squads.GDIAir, HawthorneGDI)
	if IsHardOrAbove() then
		InitAirAttackSquad(Squads.AntiHeavyAir, HawthorneGDI, MissionPlayers, { "Heavy" }, "ArmorType")
		InitAirAttackSquad(Squads.AirToAir, HawthorneGDI, MissionPlayers, { "Aircraft" }, "ArmorType")
	end
end
