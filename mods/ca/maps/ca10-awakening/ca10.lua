
-- Squads

AttackPaths = {
	{ AttackSpawn1.Location, AttackDest1.Location },
	{ AttackSpawn2.Location, AttackDest2.Location },
	{ AttackSpawn3.Location, AttackDest3.Location },
	{ AttackSpawn4.Location, AttackDest4.Location },
	{ AttackSpawn5.Location, AttackDest5.Location },
	{ AttackSpawn6.Location, AttackDest6.Location },
	{ AttackSpawn7.Location, AttackDest7.Location },
	{ AttackSpawn8.Location, AttackDest8.Location },
	{ AttackSpawn9.Location, AttackDest9.Location },
	{ AttackSpawn9.Location, AttackDest10.Location },
}

HaloDropPaths = {
	{ HaloDropSpawn1.Location, HaloDropLanding1.Location },
	{ HaloDropSpawn1.Location, HaloDropLanding2.Location },
	{ HaloDropSpawn1.Location, HaloDropLanding3.Location },
	{ HaloDropSpawn2.Location, HaloDropLanding4.Location },
	{ HaloDropSpawn3.Location, HaloDropLanding5.Location },
	{ HaloDropSpawn3.Location, HaloDropLanding6.Location },
	{ HaloDropSpawn3.Location, HaloDropLanding7.Location },
	{ HaloDropSpawn4.Location, HaloDropLanding8.Location },
	{ HaloDropSpawn4.Location, HaloDropLanding9.Location },
	{ HaloDropSpawn4.Location, HaloDropLanding10.Location },
	{ HaloDropSpawn4.Location, HaloDropLanding11.Location },
}

CyborgFactories = { CyborgFactory1, CyborgFactory2, CyborgFactory3, CyborgFactory4 }
CyborgTypes = { "n1c", "n1c", "n3c", "n5", "acol", "tplr", "enli", "rmbc" }
CyborgRallyPoints = { CyborgRally1.Location, CyborgRally2.Location }
MaxCyborgWaves = 50

GroundAttackCompositions = {
	easy = {
		{ Units = { "e1", "e1", "e1", "e2", "e3", "btr.ai", "e1", "e1" } },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "katy", "e1", "e1" }, MinTime = DateTime.Minutes(6) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "v2rl", "e1", "e1" }, MinTime = DateTime.Minutes(8) },
		{ Units = { "shok", "shok", "shok", "ttnk", "ttra", "shok" }, MinTime = DateTime.Minutes(9) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "v3rl", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(12) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "4tnk", "3tnk" }, MinTime = DateTime.Minutes(14) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "v2rl", "v3rl" }, MinTime = DateTime.Minutes(17) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "apoc", "v3rl", "btr.ai" }, MinTime = DateTime.Minutes(19) },
	},
	normal = {
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "btr.ai", "e3", "e1", "e2", "e1" } },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "katy", "e3", "e1", "e2", "e1" }, MinTime = DateTime.Minutes(4) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "v2rl", "btr.ai" }, MinTime = DateTime.Minutes(6) },
		{ Units = { "shok", "shok", "shok", "shok", "shok", "ttnk", "shok", "ttra", "shok" }, MinTime = DateTime.Minutes(8) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "3tnk", "3tnk", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(9) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "4tnk", "v3rl", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(10) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "v3rl", "v3rl", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(17) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "apoc", "v3rl", "btr.ai", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(18) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e8", "4tnk.erad", "v3rl", "btr.ai", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(19) },
	},
	hard = {
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "3tnk", "btr.ai", "e3", "e1", "e2", "e1", "e1" }, MaxTime = DateTime.Minutes(5), MaxTime = DateTime.Minutes(19) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "3tnk", "v2rl", "btr.ai", "e3", "e1", "e2", "e1", "e1" }, MinTime = DateTime.Minutes(4), MaxTime = DateTime.Minutes(8) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e1", "btr.ai", "3tnk", "ttra", "e3", "e1", "e2", "e1", "e1" }, MinTime = DateTime.Minutes(4), MaxTime = DateTime.Minutes(17) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "4tnk", "btr.ai", "shok", "e8", "katy" }, MinTime = DateTime.Minutes(6), MaxTime = DateTime.Minutes(18) },
		{ Units = { "shok", "shok", "shok", "shok", "shok", "ttnk", "ttnk", "shok", "ttra", "shok" }, MinTime = DateTime.Minutes(7) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "e3", "3tnk", "3tnk", "btr.ai", "e2", "e3", "e1", "e1" }, MinTime = DateTime.Minutes(8) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "4tnk", "v3rl", "v2rl", "shok", "e3", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(8) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "3tnk", "3tnk", "e3", "3tnk", "btr.ai", "e1", "e1", "e2" }, MinTime = DateTime.Minutes(9) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "shok", "apoc", "4tnk", "btr.ai", "v3rl" }, MinTime = DateTime.Minutes(10) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "3tnk", "4tnk", "btr.ai", "btr.ai", "v3rl", "v3rl" }, MinTime = DateTime.Minutes(15) },
		{ Units = { "e1", "e1", "e1", "e2", "e3", "e3", "e8", "e8", "4tnk.erad", "4tnk", "btr.ai", "btr.ai", "v3rl", "v3rl" }, MinTime = DateTime.Minutes(17) },
	},
}

AirAttackCompositions = {
	easy = {
		{ "mig" },
		{ "yak" },
		{ "hind" },
		{ "suk" },
	},
	normal = {
		{ "mig", "mig" },
		{ "yak", "hind" },
		{ "hind", "mig" },
		{ "suk", "yak" },
		{ "kiro" },
	},
	hard = {
		{ "mig", "mig", "mig" },
		{ "yak", "yak", "hind" },
		{ "hind", "hind", "hind" },
		{ "suk", "suk", "hind" },
		{ "kiro", "kiro", "mig" },
	}
}

GroundAttackInterval = {
	easy = DateTime.Seconds(30),
	normal = DateTime.Seconds(26),
	hard = DateTime.Seconds(22)
}

HaloDropStart = {
	easy = DateTime.Minutes(9),
	normal = DateTime.Minutes(7),
	hard = DateTime.Minutes(5)
}

HaloDropInterval = {
	easy = DateTime.Seconds(120),
	normal = DateTime.Seconds(70),
	hard = DateTime.Seconds(40)
}

AirAttackStart = {
	easy = DateTime.Minutes(10),
	normal = DateTime.Minutes(8),
	hard = DateTime.Minutes(6)
}

AirAttackInterval = {
	easy = DateTime.Minutes(2),
	normal = DateTime.Minutes(2),
	hard = DateTime.Seconds(2)
}

HoldOutTime = {
	easy = DateTime.Minutes(25),
	normal = DateTime.Minutes(25),
	hard = DateTime.Minutes(25)
}

-- Setup and Tick

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	USSR = Player.GetPlayer("USSR")
	MissionPlayer = Nod
	TimerTicks = HoldOutTime[Difficulty]
	CyborgWaves = 0

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(Nod)
	InitUSSR()

	ObjectiveProtectTemple = Nod.AddObjective("Protect Temple Prime.")

	if Difficulty == "hard" then
		Utils.Do(Nod.GetActorsByType("mlrs"), function(a)
			a.Destroy()
		end)
	end

	Trigger.OnKilled(TemplePrime, function(self, killer)
		if not Nod.IsObjectiveCompleted(ObjectiveProtectTemple) then
			Nod.MarkFailedObjective(ObjectiveProtectTemple)
		end
	end)
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		USSR.Cash = 7500
		USSR.Resources = 7500

		if TimerTicks > 0 then
			if TimerTicks > 25 then
				TimerTicks = TimerTicks - 25
			else
				TimerTicks = 0
			end

			UserInterface.SetMissionText("Protect Temple Prime - Time Remaining: " .. Utils.FormatTime(TimerTicks), HSLColor.Yellow)

		elseif not Nod.IsObjectiveCompleted(ObjectiveProtectTemple) then
			UserInterface.SetMissionText("Destroy all Soviet forces.", HSLColor.Yellow)
			ObjectiveDestroySovietForces = Nod.AddObjective("Destroy all Soviet forces.")
			Nod.MarkCompletedObjective(ObjectiveProtectTemple)

			Actor.Create("advcyborg.upgrade", true, { Owner = Nod, Location = UpgradeCreationLocation })
			Actor.Create("cyborgspeed.upgrade", true, { Owner = Nod, Location = UpgradeCreationLocation })
			Actor.Create("cyborgarmor.upgrade", true, { Owner = Nod, Location = UpgradeCreationLocation })
			DeployCyborgs()

			if ObjectiveDestroyBases ~= nil then
				local sovietBuildings = USSR.GetActorsByTypes({ "mcv", "fact", "proc", "tsla" })
				if #sovietBuildings == 0 then
					Nod.MarkCompletedObjective(ObjectiveDestroyBases)
				else
					Nod.MarkFailedObjective(ObjectiveDestroyBases)

					Utils.Do(sovietBuildings, function(a)
						a.Sell()
					end)
				end
			end
		end

		if CyborgWaves >= MaxCyborgWaves then
			if USSR.HasNoRequiredUnits() then
				Nod.MarkCompletedObjective(ObjectiveDestroySovietForces)
			end
		end
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR, 15)
	SetupRefAndSilosCaptureCredits(USSR)

	Actor.Create("POWERCHEAT", true, { Owner = USSR, Location = UpgradeCreationLocation })
	Actor.Create("tarc.upgrade", true, { Owner = USSR, Location = UpgradeCreationLocation })

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Trigger.AfterDelay(GroundAttackInterval[Difficulty], function()
		DoGroundAttack()
	end)

	Trigger.AfterDelay(AirAttackStart[Difficulty], function()
		DoAirAttack()
	end)

	Trigger.AfterDelay(HaloDropStart[Difficulty], function()
		DoHaloDrop()
	end)

	BaseAttemptLocations = {
		{ SpawnLocation = AttackSpawn8.Location, DeployLocation = ConyardPosition1.Location, RefLocation = RefPosition1.Location, TeslaCoilLocation = TeslaCoilPosition1.Location },
		{ SpawnLocation = AttackSpawn5.Location, DeployLocation = ConyardPosition2.Location, RefLocation = RefPosition2.Location, TeslaCoilLocation = TeslaCoilPosition2.Location },
		{ SpawnLocation = AttackSpawn2.Location, DeployLocation = ConyardPosition3.Location, RefLocation = RefPosition3.Location, TeslaCoilLocation = TeslaCoilPosition3.Location },
	}

	BaseAttemptTimes = { DateTime.Seconds(543), DateTime.Seconds(755), DateTime.Seconds(954) }
	BaseAttemptTimes = { DateTime.Seconds(5), DateTime.Seconds(10), DateTime.Seconds(15) }
	local attemptCount = 0

	Utils.Do(Utils.Shuffle(BaseAttemptLocations), function(attempt)
		attemptCount = attemptCount + 1
		Trigger.AfterDelay(BaseAttemptTimes[attemptCount], function()
			if ObjectiveDestroyBases == nil then
				ObjectiveDestroyBases = Nod.AddSecondaryObjective("Crush any Soviet attempts to establish a base.")
			else
				Notification("The Soviets are attempting to set up another base.")
			end
			Reinforcements.Reinforce(USSR, { "mcv" }, { attempt.SpawnLocation, attempt.DeployLocation }, 0, function(a)
				a.Deploy()
				Trigger.AfterDelay(DateTime.Seconds(2), function()
					Actor.Create("proc", true, { Owner = USSR, Location = attempt.RefLocation })
				end)
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Actor.Create("tsla", true, { Owner = USSR, Location = attempt.TeslaCoilLocation })
				end)
			end)
		end)
	end)
end

DoGroundAttack = function(isAdditional)
	local randomAttackPath = Utils.Random(AttackPaths)
	local difficultyCompositions = GroundAttackCompositions[Difficulty]
	local validCompositions = Utils.Where(difficultyCompositions, function(c)
		return (c.MinTime == nil or DateTime.GameTime >= c.MinTime) and (c.MaxTime == nil or DateTime.GameTime <= c.MaxTime)
	end)
	if #validCompositions > 0 then
		local randomComposition = Utils.Random(validCompositions)

		local units = Reinforcements.Reinforce(USSR, randomComposition.Units, randomAttackPath, 25, function(a)
			a.Scatter()
			a.Scatter()
			Trigger.AfterDelay(DateTime.Seconds(2), function()
				AssaultPlayerBaseOrHunt(a)
			end)
		end)

		if not isAdditional and CyborgWaves < MaxCyborgWaves then
			Trigger.AfterDelay(GroundAttackInterval[Difficulty], DoGroundAttack)

			if DateTime.GameTime >= DateTime.Minutes(24) then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					DoGroundAttack(true)
				end)
			end
			if DateTime.GameTime >= DateTime.Minutes(23) then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					DoGroundAttack(true)
				end)
			end
			if DateTime.GameTime >= DateTime.Minutes(22) then
				Trigger.AfterDelay(DateTime.Seconds(5), function()
					DoGroundAttack(true)
				end)
			end
		end
	end
end

DoAirAttack = function()
	local randomAttackPath = Utils.Random(AttackPaths)
	local randomComposition = Utils.Random(AirAttackCompositions[Difficulty])

	local units = Reinforcements.Reinforce(USSR, randomComposition, randomAttackPath, 25, function(a)
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			InitializeAttackAircraft(a, Nod, { "nuke", "nuk2", "obli", "gun.nod", "mlrs", "arty.nod", "harv.td", "ltnk" })
		end)
	end)

	if CyborgWaves < MaxCyborgWaves then
		Trigger.AfterDelay(AirAttackInterval[Difficulty], DoAirAttack)
	end
end

DoHaloDrop = function()
	local entryPath = Utils.Random(HaloDropPaths)
	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }

	if Difficulty == "hard" and DateTime.GameTime > DateTime.Minutes(15) then
		haloDropUnits = { "e1", "e1", "e1", "e1", "e2", "e2", "e3", "e3", "e4", "shok" }
	end

	DoHelicopterDrop(USSR, entryPath, "halo.paradrop", haloDropUnits, AssaultPlayerBaseOrHunt, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(entryPath[1])
				t.Destroy()
			end
		end)
	end)

	if CyborgWaves < MaxCyborgWaves then
		Trigger.AfterDelay(HaloDropInterval[Difficulty], DoHaloDrop)
	end
end

DeployCyborgs = function()
	if CyborgWaves == 0 then
		CyborgFactory1.RallyPoint = CyborgRally1.Location
		CyborgFactory2.RallyPoint = CyborgRally2.Location
		CyborgFactory3.RallyPoint = CyborgRally1.Location
		CyborgFactory4.RallyPoint = CyborgRally2.Location
	end

	Utils.Do(CyborgFactories, function(f)
		if not f.IsDead then
			local randomCyborg = Utils.Random(CyborgTypes)
			f.Produce(randomCyborg)
		end
	end)
	TemplePrime.Produce("rmbc")
	CyborgWaves = CyborgWaves + 1

	if CyborgWaves < MaxCyborgWaves then
		Trigger.AfterDelay(DateTime.Seconds(2), DeployCyborgs)
	end
end
