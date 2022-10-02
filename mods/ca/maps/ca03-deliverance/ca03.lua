-- Locations

SovietMainAttackPaths = {
	{ EastAssembly.Location, NorthAttackRally.Location },
	{ EastAssembly.Location, NorthEastAttackRally.Location },
	{ EastAssembly.Location, EastAttackRally.Location }
}

SovietNorthernAttackPaths = {
	{ NorthAssembly.Location, NorthAttackRally.Location },
	{ NorthAssembly.Location, NorthEastAttackRally.Location },
	{ NorthAssembly.Location, EastAttackRally.Location }
}

SovietNavalAttackPaths = { { NavalSouthAssembly.Location, NavalSouthRally.Location, NavalForwardRally.Location } }

SovietNavalFallbackAttackPaths = { { NavalEastAssembly.Location, NavalSouthEastAssembly.Location, NavalSouthAssembly.Location, NavalSouthRally.Location, NavalForwardRally.Location  } }

SovietCenterPatrolPath = { CentralPatrol1.Location, CentralPatrol2.Location, CentralPatrol3.Location, CentralPatrol4.Location, CentralPatrol3.Location, CentralPatrol2.Location }

SovietShorePatrolPath = { ShorePatrol1.Location, ShorePatrol2.Location }

SovietHindPatrolPath = { NavalEastAssembly.Location, NavalSouthEastAssembly.Location, NavalSouthAssembly.Location, NavalSouthRally.Location, NavalForwardRally.Location, EastAssembly.Location, CentralPatrol3.Location }

HaloDropPaths = {
	{ HaloDrop1Spawn.Location, HaloDrop1Landing.Location },
	{ HaloDrop2Spawn.Location, HaloDrop2Spawn.Location }
}

NavalDropPaths = {
	{ RaidSpawn.Location, RaidLanding1.Location },
	{ RaidSpawn.Location, RaidLanding2.Location }
}

-- Other Variables

HaloDropStart = {
	easy = DateTime.Minutes(14),
	normal = DateTime.Minutes(12),
	hard = DateTime.Minutes(10)
}

HaloDropInterval = {
	easy = DateTime.Minutes(3),
	normal = DateTime.Minutes(2),
	hard = DateTime.Minutes(1)
}

NavalDropStart = {
	easy = DateTime.Minutes(17),
	normal = DateTime.Minutes(15),
	hard = DateTime.Minutes(13)
}

NavalDropInterval = {
	easy = DateTime.Minutes(4),
	normal = DateTime.Minutes(3),
	hard = DateTime.Minutes(2)
}

HoldOutTime = {
	easy = DateTime.Minutes(8),
	normal = DateTime.Minutes(9),
	hard = DateTime.Minutes(10)
}

-- Squads

Squads = {
	MainBasic = {
		Player = nil,
		Delay = {
			easy = DateTime.Seconds(30),
			normal = DateTime.Seconds(15),
			hard = DateTime.Seconds(5)
		},
		Interval = {
			easy = DateTime.Seconds(30),
			normal = DateTime.Seconds(20),
			hard = DateTime.Seconds(10)
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { SovietMainBarracks1, SovietMainBarracks2 }, Vehicles = { SovietMainFactory1, SovietMainFactory2 } },
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
		Units = {
			easy = {
				Infantry = { "e3", "e1", "e1", "e1", "e2", "e4" },
				Vehicles = { "3tnk", "btr" }
			},
			normal = {
				Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" },
				Vehicles = { "3tnk", "btr.ai", "btr" }
			},
			hard = {
				Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4" },
				Vehicles = { "3tnk", "btr.ai", "3tnk" }
			}
		},
		AttackPaths = SovietMainAttackPaths,
		TransitionTo = {
			SquadType = "MainAdvanced",
			GameTime = {
				easy = DateTime.Minutes(17),
				normal = DateTime.Minutes(15),
				hard = DateTime.Minutes(12)
			}
		}
	},
	MainAdvanced = {
		Player = nil,
		Interval = {
			easy = DateTime.Seconds(30),
			normal = DateTime.Seconds(20),
			hard = DateTime.Seconds(10)
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { SovietMainBarracks1, SovietMainBarracks2 }, Vehicles = { SovietMainFactory1, SovietMainFactory2 } },
		ProducerTypes = { Infantry = { "barr" }, Vehicles = { "weap" } },
		Units = {
			easy = {
				Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" },
				Vehicles = { "4tnk", "btr.ai" }
			},
			normal = {
				Infantry = { "e3", "e1", "e1", "shok", "shok", "e1", "e2", "e3", "e4" },
				Vehicles = { "3tnk", "4tnk", "katy" }
			},
			hard = {
				Infantry = { "e3", "e1", "e1", "e3", "shok", "e1", "shok", "e1", "e2", "e3", "e4" },
				Vehicles = { "3tnk", "4tnk", "btr.ai", "katy", "ttra" }
			}
		},
		AttackPaths = SovietMainAttackPaths
	},
	Northern = {
		Player = nil,
		Delay = {
			easy = DateTime.Seconds(30),
			normal = DateTime.Seconds(15),
			hard = DateTime.Seconds(5)
		},
		Interval = {
			easy = DateTime.Seconds(25),
			normal = DateTime.Seconds(20),
			hard = DateTime.Seconds(15)
		},
		QueueProductionStatuses = {
			Infantry = false,
			Vehicles = false
		},
		FollowLeader = true,
		IdleUnits = { },
		ProducerActors = { Infantry = { SovietNorthBarracks1, SovietNorthBarracks2 }, Vehicles = { SovietNorthFactory } },
		Units = {
			easy = {
				Infantry = { "e3", "e1", "e1", "e1", "e2" },
				Vehicles = { "btr" }
			},
			normal = {
				Infantry = { "e3", "e1", "e1", "e1", "e1", "e2", "e4" },
				Vehicles = { "3tnk", "btr.ai" }
			},
			hard = {
				Infantry = { "e3", "e1", "e1", "e1", "e1", "e1", "e2", "e3", "e4" },
				Vehicles = { "3tnk", "btr.ai", "3tnk" }
			}
		},
		AttackPaths = SovietNorthernAttackPaths
	},
	Migs = {
		Player = nil,
		Delay = {
			easy = DateTime.Minutes(13),
			normal = DateTime.Minutes(11),
			hard = DateTime.Minutes(9)
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
		ProducerTypes = { Aircraft = { "afld" } },
		Units = {
			easy = {
				Aircraft = { "mig" }
			},
			normal = {
				Aircraft = { "mig", "mig" }
			},
			hard = {
				Aircraft = { "mig", "mig", "mig" }
			}
		},
	},
	Naval = {
		Player = nil,
		ActiveCondition = function()
			local navalProductionBuildings = Greece.GetActorsByTypes({ "syrd", "spen" })
			return #navalProductionBuildings > 0
		end,
		Interval = {
			easy = DateTime.Seconds(45),
			normal = DateTime.Seconds(30),
			hard = DateTime.Seconds(15)
		},
		QueueProductionStatuses = {
			Ships = false
		},
		IdleUnits = { },
		ProducerActors = { Ships = { SovietSouthSubPen1, SovietSouthSubPen2 } },
		ProducerTypes = { Ships = { "spen" } },
		Units = {
			easy = { Ships = { "ss", "seas" } },
			normal = { Ships = { "ss", "seas" } },
			hard = { Ships = { "ss", "ss", "seas" } }
		},
		AttackPaths = SovietNavalAttackPaths
	}
}

-- Setup and Tick

WorldLoaded = function()
	Greece = Player.GetPlayer("Greece")
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	TimerTicks = 0
	GDICommanderAlive = true

	InitObjectives(Greece)
	InitUSSR()
	Camera.Position = PlayerStart.CenterPosition

	ObjectiveFindBase = Greece.AddObjective("Find besieged GDI base.")
	UserInterface.SetMissionText("Find besieged GDI base.", HSLColor.Yellow)

	if Difficulty ~= "hard" then
		SovietMammoth1.Destroy()
		SovietV22.Destroy()
	end

	if Difficulty == "easy" then
		SovietMammoth2.Destroy()
		SovietV21.Destroy()
	end

	-- On finding the GDI base, transfer ownership to player
	Trigger.OnEnteredProximityTrigger(GDIRefinery.CenterPosition, WDist.New(9 * 1024), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)

			local gdiForces = GDI.GetActors()
			Utils.Do(gdiForces, function(a)
				if a.Type ~= "player" then
					a.Owner = Greece
				end
			end)

			InitUSSRAttacks()

			Trigger.AfterDelay(DateTime.Seconds(1), function()
				Actor.Create("QueueUpdaterDummy", true, { Owner = Greece, Location = GDIBaseCenter.Location })
				ObjectiveHoldOut = Greece.AddObjective("Hold out until reinforcements arrive.")
				UserInterface.SetMissionText("Hold out until reinforcements arrive.", HSLColor.Yellow)
				Greece.MarkCompletedObjective(ObjectiveFindBase)
			end)

			Trigger.AfterDelay(HoldOutTime[Difficulty] - DateTime.Seconds(20), function()
				McvFlare = Actor.Create("flare", true, { Owner = Greece, Location = McvRally.Location })
				Media.PlaySpeechNotification(Greece, "SignalFlare")
				Beacon.New(Greece, McvRally.CenterPosition)
				Trigger.AfterDelay(DateTime.Seconds(20), function()
					McvFlare.Destroy()
				end)
			end)

			Trigger.AfterDelay(HoldOutTime[Difficulty], function()
				ObjectiveLocateCommander = Greece.AddObjective("Locate the GDI commander.")
				Greece.MarkCompletedObjective(ObjectiveHoldOut)
				UserInterface.SetMissionText("Locate the GDI commander.", HSLColor.Yellow)

				Trigger.AfterDelay(DateTime.Seconds(1), function()
					Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
					Reinforcements.Reinforce(Greece, { "mcv" }, { McvEntry.Location, McvRally.Location })
					Beacon.New(Greece, McvRally.CenterPosition)
				end)
			end)
		end
	end)

	-- On proximity to prison, reveal it and update objectives.
	Trigger.OnEnteredProximityTrigger(SovietPrison.CenterPosition, WDist.New(10 * 1024), function(a, id)
		if a.Owner == Greece then
			Trigger.RemoveProximityTrigger(id)
			Beacon.New(Greece, GDICommanderSpawn.CenterPosition)
			ObjectiveCapturePrison = Greece.AddObjective("Take control of prison and rescue GDI commander.")

			if ObjectiveLocateCommander ~= nil then
				Greece.MarkCompletedObjective(ObjectiveLocateCommander)
			end

			UserInterface.SetMissionText("Take control of prison and rescue GDI commander.", HSLColor.Yellow)
			PrisonCamera = Actor.Create("camera.paradrop", true, { Owner = Greece, Location = SovietPrison.Location })

			Trigger.AfterDelay(DateTime.Seconds(5), function()
				PrisonCamera.Destroy()
			end)
		end
	end)

	Trigger.OnCapture(SovietPrison, function(self, captor, oldOwner, newOwner)
		if newOwner == Greece then
			local commander = Reinforcements.Reinforce(GDI, { "gnrl" }, { GDICommanderSpawn.Location, GDICommanderRally.Location })[1]

			Trigger.OnKilled(commander, function(self, killer)
				GDICommanderAlive = false
			end)

			Trigger.AfterDelay(DateTime.Seconds(3), function()
				if GDICommanderAlive then
					Media.PlaySpeechNotification(Greece, "TargetFreed")
				end

				Trigger.AfterDelay(DateTime.Seconds(3), function()
					Media.PlaySpeechNotification(Greece, "AlliedReinforcementsWest")
					Reinforcements.ReinforceWithTransport(GDI, "tran", nil, { GDIRescueSpawn.Location, GDIRescueRally.Location }, nil, function(transport, cargo)

						Trigger.AfterDelay(DateTime.Seconds(1), function()
							if not commander.IsDead then
								commander.EnterTransport(transport)
							end
						end)

						Trigger.OnPassengerEntered(transport, function(t, passenger)
							Media.PlaySpeechNotification(Greece, "TargetRescued")
							t.Move(GDIReinforcementsEntry.Location)
							t.Destroy()
							Trigger.AfterDelay(DateTime.Seconds(7), function()
								Greece.MarkCompletedObjective(ObjectiveCapturePrison)
							end)
						end)
					end)
				end)
			end)
		end
	end)

	Trigger.OnKilled(SovietPrison, function(self, killer)
		if self.Owner ~= Greece then
			GDICommanderAlive = false
		end
	end)
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 then
		OncePerSecondChecks()
	end
end

OncePerSecondChecks = function()
	USSR.Cash = 5000
	USSR.Resources = 5000

	if TimerTicks > 0 then
		if TimerTicks > 25 then
			TimerTicks = TimerTicks - 25
		else
			TimerTicks = 0
		end
	end

	if PlayerForcesDefeated() or not GDICommanderAlive then
		if ObjectiveFindBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveFindBase) then
			Greece.MarkFailedObjective(ObjectiveFindBase)
		end
		if ObjectiveHoldOut ~= nil and not Greece.IsObjectiveCompleted(ObjectiveHoldOut) then
			Greece.MarkFailedObjective(ObjectiveHoldOut)
		end
		if ObjectiveLocateCommander ~= nil and not Greece.IsObjectiveCompleted(ObjectiveLocateCommander) then
			Greece.MarkFailedObjective(ObjectiveLocateCommander)
		end
		if ObjectiveCapturePrison ~= nil and not Greece.IsObjectiveCompleted(ObjectiveCapturePrison) then
			Greece.MarkFailedObjective(ObjectiveCapturePrison)
		end
	end
end

-- Functions

PlayerForcesDefeated = function()
	if ObjectiveFindBase ~= nil and not Greece.IsObjectiveCompleted(ObjectiveFindBase) then
		local playerActors = Greece.GetActors()
		return #playerActors == 0
	else
		return Greece.HasNoRequiredUnits()
	end
end

InitUSSR = function()
	AutoRepairAndRebuildBuildings(USSR)
	SetupRefAndSilosCaptureCredits(USSR)
	AutoReplaceHarvesters(USSR)
	InitUSSRPatrols()

	local ussrGroundAttackers = USSR.GetGroundAttackers()

	Utils.Do(ussrGroundAttackers, function(a)
		TargetSwapChance(a, USSR, 10)
		CallForHelpOnDamagedOrKilled(a, IsUSSRGroundHunterUnit)
	end)

	-- If main sub pens are destroyed, update naval attack path
	Utils.Do({ SovietSouthSubPen1, SovietSouthSubPen2 }, function(a)
		Trigger.OnRemovedFromWorld(a, function(self)
			if SovietSouthSubPen1.IsDead and SovietSouthSubPen2.IsDead and not SovietNorthSubPen.IsDead then
				Squads.Naval.AttackPaths = SovietNavalFallbackAttackPaths
				Squads.Naval.ProducerActors.Ships = { SovietNorthSubPen }
			end
		end)
	end)

	local hinds = USSR.GetActorsByType("hind")
	Utils.Do(hinds, function(a)
		Trigger.OnDamaged(a, function(self, attacker, damage)
			if not self.IsDead and self.AmmoCount() == 0 then
				Trigger.ClearAll(self)
				self.Stop()
				self.ReturnToBase()
				Trigger.AfterDelay(DateTime.Seconds(1), function()
					self.Patrol(SovietHindPatrolPath, true)
				end)
			end
		end)
	end)
end

InitUSSRPatrols = function()
	local centerPatrollers = { SovietCenterPatroller1, SovietCenterPatroller2, SovietCenterPatroller3, SovietCenterPatroller4, SovietCenterPatroller5, SovietCenterPatroller6, SovietCenterPatroller7, SovietCenterPatroller8 }
	local shorePatrollers = { SovietShorePatroller1, SovietShorePatroller2, SovietShorePatroller3, SovietShorePatroller4, SovietShorePatroller5, SovietShorePatroller6, SovietShorePatroller7 }
	local hindPatrollers = { SovietHindPatroller1, SovietHindPatroller2 }

	Utils.Do(centerPatrollers, function(unit)
		if not unit.IsDead then
			unit.Patrol(SovietCenterPatrolPath, true, 100)
		end
	end)

	Utils.Do(shorePatrollers, function(unit)
		if not unit.IsDead then
			unit.Patrol(SovietShorePatrolPath, true, 100)
		end
	end)

	Utils.Do(hindPatrollers, function(unit)
		if not unit.IsDead then
			unit.Patrol(SovietHindPatrolPath, true)
		end
	end)
end

InitUSSRAttacks = function()
	Trigger.AfterDelay(Squads.MainBasic.Delay[Difficulty], function()
		InitAttackSquad(Squads.MainBasic, USSR)
	end)

	Trigger.AfterDelay(Squads.Northern.Delay[Difficulty], function()
		InitAttackSquad(Squads.Northern, USSR)
	end)

	Trigger.AfterDelay(Squads.Migs.Delay[Difficulty], function()
		InitAirAttackSquad(Squads.Migs, USSR, Greece, { "harv", "harv.td", "pbox", "pris", "ptnk", "mtnk", "2tnk" })
	end)

	InitAttackSquad(Squads.Naval, USSR)


	Trigger.AfterDelay(HaloDropStart[Difficulty], function()
		DoHaloDrop()
	end)

	Trigger.AfterDelay(NavalDropStart[Difficulty], function()
		DoNavalDrop()
	end)
end

IsUSSRGroundHunterUnit = function(actor)
	return actor.Owner == USSR and actor.HasProperty("Move") and not actor.HasProperty("Land") and actor.HasProperty("Hunt") and actor.Type ~= "v2rl" and actor.Type ~= "katy"
end

DoHaloDrop = function()
	local entryPath = Utils.Random(HaloDropPaths)

	local haloDropUnits = { "e1", "e1", "e1", "e2", "e3", "e4" }
	if Difficulty == "hard" and DateTime.GameTime > DateTime.Minutes(15) then
		haloDropUnits = { "e1", "e1", "e1", "e1", "e2", "e2", "e3", "e3", "e4", "shok" }
	end

	DoHelicopterDrop(USSR, entryPath, "halo.paradrop", haloDropUnits, AssaultPlayerBase, function(t)
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			if not t.IsDead then
				t.Move(entryPath[1])
				t.Destroy()
			end
		end)
	end)

	Trigger.AfterDelay(HaloDropInterval[Difficulty], DoHaloDrop)
end

DoNavalDrop = function()
	local navalDropPath = Utils.Random(NavalDropPaths)
	local navalDropExitPath = { navalDropPath[2], navalDropPath[1] }
	local navalDropUnits = { "3tnk", "v2rl", "3tnk", "btr.ai" }

	DoNavalTransportDrop(USSR, navalDropPath, navalDropExitPath, "lst", navalDropUnits, AssaultPlayerBase)

	Trigger.AfterDelay(NavalDropInterval[Difficulty], DoNavalDrop)
end

AssaultPlayerBase = function(actor)
	if not actor.IsDead then
		actor.Patrol({ GDIBaseCenter.Location, ShoreCenter.Location })
	end
	IdleHunt(actor)
end