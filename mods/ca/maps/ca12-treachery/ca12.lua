
GreeceMainAttackPaths = {
	{ SouthAttackRally.Location, SouthAttack1.Location },
	{ EastAttackRally.Location, EastAttack1.Location, EastAttack2.Location, EastAttack3a.Location },
	{ EastAttackRally.Location, EastAttack1.Location, EastAttack2.Location, EastAttack3b.Location },
}

TraitorUnits = {
	easy = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "btr" }, MaxTime = DateTime.Minutes(14) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok" }, Vehicles = { "btr", "katy" }, MinTime = DateTime.Minutes(14) },
	},
	normal = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "3tnk" }, MaxTime = DateTime.Minutes(12) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok" }, Vehicles = { "3tnk", "katy" }, MinTime = DateTime.Minutes(12) },
	},
	hard = {
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1" }, Vehicles = { "3tnk", "btr" }, MaxTime = DateTime.Minutes(10) },
		{ Infantry = { "e3", "e1", "e1", "e1", "e3", "e1", "shok" }, Vehicles = { "3tnk", "v2rl", "ttra" }, MinTime = DateTime.Minutes(10) },
	},
}

Squads = {
	Main = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(4),
			normal = DateTime.Minutes(3),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 20 }, { MinTime = DateTime.Minutes(14), Value = 40 } },
			normal = { { MinTime = 0, Value = 34 }, { MinTime = DateTime.Minutes(12), Value = 68 } },
			hard = { { MinTime = 0, Value = 52 }, { MinTime = DateTime.Minutes(10), Value = 105 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { AlliedSouthBarracks }, Vehicles = { AlliedSouthFactory } },
		Units = UnitCompositions.Allied.Main,
		AttackPaths = GreeceMainAttackPaths,
	},
	Traitor = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(7),
			normal = DateTime.Minutes(5),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 10 }, { MinTime = DateTime.Minutes(14), Value = 20 } },
			normal = { { MinTime = 0, Value = 16 }, { MinTime = DateTime.Minutes(12), Value = 32 } },
			hard = { { MinTime = 0, Value = 28 }, { MinTime = DateTime.Minutes(10), Value = 55 } },
		},
		QueueProductionStatuses = { Infantry = false, Vehicles = false },
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { TraitorBarracks }, Vehicles = { TraitorFactory } },
		Units = TraitorUnits,
		AttackPaths = GreeceMainAttackPaths,
	},
	Air = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(12),
			hard = DateTime.Minutes(11)
		},
		Interval = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Seconds(150),
			hard = DateTime.Minutes(2)
		},
		QueueProductionStatuses = {
			Aircraft = false
		},
		IdleUnits = { },
		ProducerActors = nil,
		ProducerTypes = { Aircraft = { "hpad" } },
		Units = {
			easy = {
				{ Aircraft = { "heli" } }
			},
			normal = {
				{ Aircraft = { "heli", "heli" } },
				{ Aircraft = { "harr" } }
			},
			hard = {
				{ Aircraft = { "heli", "heli", "heli" } },
				{ Aircraft = { "harr", "harr" } }
			}
		},
	}
}

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
    Greece = Player.GetPlayer("Greece")
	Traitor = Player.GetPlayer("Traitor")
	USSRAbandoned = Player.GetPlayer("USSRAbandoned")
	MissionPlayer = USSR
	TimerTicks = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(USSR)
	InitGreece()

	ObjectiveKillTraitor = USSR.AddObjective("Find and kill the traitor General Yegorov.")
	ObjectiveKeepBorisAlive = USSR.AddObjective("Boris must survive.")
	ObjectiveFindSovietBase = USSR.AddSecondaryObjective("Take control of abandoned Soviet base.")

	AbandonedHalo.ReturnToBase(AbandonedHelipad)

	Trigger.OnCapture(AbandonedHelipad, function(self, captor, oldOwner, newOwner)
		if newOwner == USSR then
			AbandonedHalo.Owner = USSR
		end
	end)

	Trigger.OnEnteredProximityTrigger(AbandonedBaseCenter.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.Owner == USSR and not a.HasProperty("Land") then
			Trigger.RemoveProximityTrigger(id)
			AbandonedBaseDiscovered()
		end
	end)

	Trigger.OnKilled(Bodyguard1, function(self, killer)
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			if not TraitorGeneral.IsDead then
				TraitorGeneral.Move(TraitorGeneralSafePoint.Location)
			end
		end)
	end)

	Trigger.OnKilled(TraitorGeneral, function(self, killer)
		USSR.MarkCompletedObjective(ObjectiveKillTraitor)
		USSR.MarkCompletedObjective(ObjectiveKeepBorisAlive)
	end)

	Trigger.OnKilled(Boris, function(self, killer)
		USSR.MarkFailedObjective(ObjectiveKeepBorisAlive)
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Greece.Cash = 3500
		Greece.Resources = 3500
		Traitor.Cash = 3500
		Traitor.Resources = 3500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end
		end

		if Boris.IsDead then
			if ObjectiveKeepBorisAlive ~= nil and not USSR.IsObjectiveCompleted(ObjectiveKeepBorisAlive) then
				USSR.MarkFailedObjective(ObjectiveKeepBorisAlive)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitGreece = function()
	AutoRepairAndRebuildBuildings(Greece, 10)
	SetupRefAndSilosCaptureCredits(Greece)
	AutoReplaceHarvesters(Greece)

	Actor.Create("POWERCHEAT", true, { Owner = Greece, Location = UpgradeCreationLocation })
	Actor.Create("POWERCHEAT", true, { Owner = Traitor, Location = UpgradeCreationLocation })

	local alliedGroundAttackers = Greece.GetGroundAttackers()

	Utils.Do(alliedGroundAttackers, function(a)
		TargetSwapChance(a, Greece, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGreeceGroundHunterUnit)
	end)

	local traitorGroundAttackers = Traitor.GetGroundAttackers()

	Utils.Do(traitorGroundAttackers, function(a)
		TargetSwapChance(a, Traitor, 10)
		CallForHelpOnDamagedOrKilled(a, WDist.New(5120), IsGroundHunterUnit)
	end)

	Actor.Create("hazmat.upgrade", true, { Owner = Greece, Location = UpgradeCreationLocation })
	Actor.Create("apb.upgrade", true, { Owner = Greece, Location = UpgradeCreationLocation })

	if Difficulty == "hard" then
		Actor.Create("cryr.upgrade", true, { Owner = Greece, Location = UpgradeCreationLocation })
	end
end

AbandonedBaseDiscovered = function()
	local baseBuildings = Map.ActorsInBox(AbandonedBaseTopLeft.CenterPosition, AbandonedBaseBottomRight.CenterPosition, function(a)
		return a.Owner == USSRAbandoned
	end)

	Utils.Do(baseBuildings, function(a)
		a.Owner = USSR
	end)

	USSR.MarkCompletedObjective(ObjectiveFindSovietBase)

	Trigger.AfterDelay(Squads.Main.Delay[Difficulty], function()
		InitAttackSquad(Squads.Main, Greece)
	end)

	Trigger.AfterDelay(Squads.Traitor.Delay[Difficulty], function()
		InitAttackSquad(Squads.Traitor, Traitor)
	end)

	Trigger.AfterDelay(Squads.Air.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Air, Greece, Nod, { "harv", "v2rl", "apwr", "tsla", "ttra", "v3rl", "mig", "hind", "suk", "suk.upg", "kiro", "apoc" })
	end)

	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = USSR, Location = UpgradeCreationLocation })
	end)
end
