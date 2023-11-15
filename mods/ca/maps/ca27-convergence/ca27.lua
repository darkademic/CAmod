MaxBreakthroughs = {
    easy = 6,
    normal = 3,
    hard = 0
}

FleetWaveCompositions = {
    easy = {
        { "pac", "pac", "deva" },
        { "pac", "deva", "deva" }
    },
    normal = {
        { "pac", "pac", "deva", "pac" },
        { "pac", "deva", "deva", "pac" },
    },
    hard = {
        { "pac", "pac", "deva", "pac", "deva" },
        { "pac", "deva", "pac", "deva", "pac" },
    }
}

TimeBetweenWaves = {
    easy = DateTime.Minutes(3),
    normal = DateTime.Minutes(3),
    hard = DateTime.Minutes(3),
}

FleetSpawns = {
    Left = { LSpawn1, LSpawn2, LSpawn3 },
    Middle = { MSpawn1, MSpawn2 },
    Right = { RSpawn1, RSpawn2 }
}

WaveSpawns = {
    FleetSpawns.Left, FleetSpawns.Left, FleetSpawns.Left, FleetSpawns.Middle, FleetSpawns.Right, FleetSpawns.Middle, FleetSpawns.Left, FleetSpawns.Right, FleetSpawns.Middle, FleetSpawns.Right
}

UnitBuildTimeMultipliers = {
	easy = 0.3,
	normal = 0.2,
	hard = 0.1,
}

LeftScrinSpawners = { ScrinSpawnerL1, ScrinSpawnerL2, ScrinSpawnerL3, ScrinSpawnerL4 }
MiddleScrinSpawners = { ScrinSpawnerM1, ScrinSpawnerM2 }

Squads = {
	ScrinMain = {
		Delay = {
			easy = DateTime.Minutes(3),
			normal = DateTime.Seconds(150),
			hard = DateTime.Minutes(2)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 15 }, { MinTime = DateTime.Minutes(14), Value = 35 } },
			normal = { { MinTime = 0, Value = 34 }, { MinTime = DateTime.Minutes(12), Value = 68 } },
			hard = { { MinTime = 0, Value = 52 }, { MinTime = DateTime.Minutes(10), Value = 105 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = LeftScrinSpawners, Vehicles = LeftScrinSpawners, Aircraft = LeftScrinSpawners },
		ProducerTypes = nil,
		Units = UnitCompositions.Scrin.Main,
		AttackPaths = {
            { LAttackRally1a.Location, LAttackRally1b.Location },
            { LAttackRally2a.Location, LAttackRally2b.Location },
            { LAttackRally3a.Location, LAttackRally3b.Location }
        },
	},
	ScrinWater = {
		Delay = {
			easy = DateTime.Minutes(5),
			normal = DateTime.Minutes(4),
			hard = DateTime.Minutes(3)
		},
		AttackValuePerSecond = {
			easy = { { MinTime = 0, Value = 5 }, { MinTime = DateTime.Minutes(14), Value = 15 } },
			normal = { { MinTime = 0, Value = 16 }, { MinTime = DateTime.Minutes(12), Value = 32 } },
			hard = { { MinTime = 0, Value = 28 }, { MinTime = DateTime.Minutes(10), Value = 55 } },
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false,
			Aircraft = false,
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = MiddleScrinSpawners, Vehicles = MiddleScrinSpawners, Aircraft = MiddleScrinSpawners },
		ProducerTypes = nil,
		Units = {
			easy = {
				{ Vehicles = { "intl", "seek" }, },
				{ Vehicles = { "seek", "seek" }, },
				{ Vehicles = { "lace", "lace" }, }
			},
			normal = {
				{ Vehicles = { "seek", "intl.ai2" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "lace" }, },
			},
			hard = {
				{ Vehicles = { "intl", "intl.ai2", "seek" }, },
				{ Vehicles = { "seek", "seek", "seek" }, },
				{ Vehicles = { "lace", "lace", "seek", "seek" }, },
				{ Vehicles = { "devo", "intl.ai2", "ruin" }, MinTime = DateTime.Minutes(7) },
			}
        },
		AttackPaths = {
            { MAttackRally1.Location },
            { MAttackRally2a.Location, MAttackRally2b.Location }
        },
	},
}

WorldLoaded = function()
    Scrin = Player.GetPlayer("Scrin")
    GDI = Player.GetPlayer("GDI")
    TibLifeforms = Player.GetPlayer("TibLifeforms")
	MissionPlayer = GDI
	TimerTicks = 0
    WavesRemaining = #WaveSpawns
    NumBreakthroughs = 0
    NextWave = 1
    FinalWaveArrived = false

	Camera.Position = PlayerStart.CenterPosition

	InitObjectives(GDI)
	InitScrin()
	SetupLightning()
	SetupIonStorm()
    UpdateMissionText()

    Trigger.AfterDelay(TimeBetweenWaves[Difficulty] + DateTime.Minutes(1), function()
        SendFleetWave()

		Trigger.AfterDelay(DateTime.Seconds(40), function()
			Notification("The area across the river is infested with Tiberium lifeforms. You will need to use aicraft to intercept Scrin fleet vessels attempting to break through there.")
			MediaCA.PlaySound("c_acrossriver.aud", 2)
			Beacon.New(GDI, AcrossRiver.CenterPosition)
            local acrossRiverCamera = Actor.Create("camera", true, { Owner = GDI, Location = AcrossRiver.Location })
            Trigger.AfterDelay(DateTime.Seconds(10), function()
                acrossRiverCamera.Destroy()
            end)
		end)
    end)

	if Difficulty == "hard" then
		Actor.Create("shields.upgrade", true, { Owner = Scrin })
	end

    if Difficulty == "hard" then
        ObjectiveStopFleet = GDI.AddObjective("Prevent any Scrin fleet ships breaking through.")
    else
        ObjectiveStopFleet = GDI.AddObjective("Allow no more than " .. MaxBreakthroughs[Difficulty] .. " fleet ships through.")
    end

    BottomOfMap = { }
    for i=1, 128 do
        table.insert(BottomOfMap, CPos.New(i,96))
    end
end

Tick = function()
	OncePerSecondChecks()
	OncePerFiveSecondChecks()
end

OncePerSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		Scrin.Resources = Scrin.ResourceCapacity - 500

        if NumBreakthroughs > MaxBreakthroughs[Difficulty] then
            GDI.MarkFailedObjective(ObjectiveStopFleet)
        end

        if FinalWaveArrived and #Scrin.GetActorsByTypes({ "pac", "deva" }) == 0 then
            GDI.MarkCompletedObjective(ObjectiveStopFleet)
        end

        UpdateMissionText()
	end
end

OncePerFiveSecondChecks = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 125 == 0 then
		UpdatePlayerBaseLocation()
	end
end

InitScrin = function()
	AutoRepairBuildings(Scrin)
	Actor.Create("ioncon.upgrade", true, { Owner = Scrin })

	Trigger.AfterDelay(Squads.ScrinMain.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinMain, Scrin)
	end)

    Trigger.AfterDelay(Squads.ScrinWater.Delay[Difficulty], function()
		InitAttackSquad(Squads.ScrinWater, Scrin)
	end)
end

SetupLightning = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(8), DateTime.Seconds(25))
	Trigger.AfterDelay(nextStrikeDelay, function()
        LightningStrike()
        SetupLightning()
	end)
end

SetupIonStorm = function()
	local nextStrikeDelay = Utils.RandomInteger(DateTime.Seconds(8), DateTime.Seconds(25))
	Trigger.AfterDelay(nextStrikeDelay, function()
        IonStorm()
        SetupIonStorm()
	end)
end

LightningStrike = function()
	local duration = Utils.RandomInteger(5, 8)
	local thunderDelay = Utils.RandomInteger(5, 65)
	local soundNumber
	Lighting.Flash("LightningStrike", duration)

	repeat
		soundNumber = Utils.RandomInteger(1, 7)
	until(soundNumber ~= LastSoundNumber)
	LastSoundNumber = soundNumber

	Trigger.AfterDelay(thunderDelay, function()
		Media.PlaySound("thunder" .. soundNumber .. ".aud")
	end)
end

IonStorm = function()
	local duration = Utils.RandomInteger(5, 8)
	local soundNumber
	Lighting.Flash("IonStrike", duration)
	repeat
		soundNumber = Utils.RandomInteger(1, 4)
	until(soundNumber ~= LastIonSoundNumber)
	LastIonSoundNumber = soundNumber
	Media.PlaySound("ionstorm" .. soundNumber .. ".aud")
end

SendFleetWave = function()
    Notification("Scrin fleet vessels approaching.")
	MediaCA.PlaySound("c_scrinfleetvessels.aud", 2)

    local composition = Utils.Random(FleetWaveCompositions[Difficulty])
    local interval = 1
    local currentWave = NextWave

    -- for each unit in the wave, get the possible base spawn points, pick one and generate offsetted entry/exit
    Utils.Do(composition, function(shipType)
        Trigger.AfterDelay(interval, function()
            local spawn = Utils.Random(WaveSpawns[currentWave])
            local xOffset = Utils.RandomInteger(-5, 5)
            local entry = spawn.Location + CVec.New(xOffset, 0)
            local exit = CPos.New(entry.X, 96)
			Beacon.New(GDI, spawn.CenterPosition + WVec.New(xOffset * 1024, 0, 0))
            Reinforcements.Reinforce(Scrin, { shipType }, { entry, exit }, 25, function(self)
                self.Destroy()
                NumBreakthroughs = NumBreakthroughs + 1
				Media.PlaySoundNotification(GDI, "AlertBuzzer")
				Notification("A Scrin fleet vessel has broken through.")
            end)
        end)
        interval = interval + DateTime.Seconds(5)
    end)

    if currentWave == #WaveSpawns then
        Trigger.AfterDelay(#composition * DateTime.Seconds(5), function()
            FinalWaveArrived = true
        end)
    end

    NextWave = NextWave + 1
    WavesRemaining = WavesRemaining - 1

    if NextWave <= #WaveSpawns then
        Trigger.AfterDelay(TimeBetweenWaves[Difficulty], function()
            SendFleetWave()
        end)
    end
end

UpdateMissionText = function()
    local missionText = "Waves remaining: " .. WavesRemaining

    if Difficulty ~= "hard" then
        missionText = missionText .. " -- Ships escaped: " .. NumBreakthroughs .. "/" .. MaxBreakthroughs[Difficulty]
    end

    local color = HSLColor.Yellow
    if Difficulty ~= "hard" and NumBreakthroughs >= MaxBreakthroughs[Difficulty] then
        color = HSLColor.Red
    end

    UserInterface.SetMissionText(missionText, color)
end
