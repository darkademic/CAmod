SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	USSR = Player.GetPlayer("USSR")
	Scrin = Player.GetPlayer("Scrin")
	ScrinRebelsInactive = Player.GetPlayer("ScrinRebelsInactive")
	Nod = Player.GetPlayer("Nod")
	NodInactive = Player.GetPlayer("NodInactive")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { USSR, Scrin }
	SinglePlayerPlayer = ScrinRebels
	ScrinRebelPlayers = GetActiveCoopPlayers({ Multi0, Multi2, Multi3, Multi5 })
	NodPlayers = GetActiveCoopPlayers({ Multi1, Multi4 })
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)

	Utils.Do(ScrinRebelPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	local actors = GetSpreadableUnits(SinglePlayerPlayer)
	AssignToCoopPlayers(actors, ScrinRebelPlayers)

	Trigger.AfterDelay(1, function()
		StopSpread = false
	end)
end

AfterTick = function()

end

TransferNodBaseUnits = function(units)
	local recipientPlayers
	if #NodPlayers > 0 then
		recipientPlayers = NodPlayers
	else
		recipientPlayers = nil
	end

	AssignToCoopPlayers(units, recipientPlayers)

	Utils.Do(units, function(a)
		if a.Type == "msg" then
			a.Undeploy()
		end
	end)
end

TransferNodBaseStructures = function(structures)
	if #NodPlayers > 0 then
		AssignToCoopPlayers(structures, NodPlayers)
	else
		Utils.Do(structures, function(a)
			a.Owner = Nod
		end)
		AutoRepairBuildings(Nod)
	end
end

TransferStrandedNodUnits = function(units)
	local recipientPlayers
	if #NodPlayers > 0 then
		recipientPlayers = NodPlayers
	else
		recipientPlayers = nil
	end

	AssignToCoopPlayers(units, recipientPlayers)

	Utils.Do(units, function(a)
		if a.Type == "msg" then
			a.Undeploy()
		end
	end)
end

TransferRebelStructures = function(structures)
	local recipientPlayer = GetFirstActivePlayer()
	Utils.Do(structures, function(a)
		a.Owner = recipientPlayer
	end)
	Trigger.AfterDelay(1, function()
		CACoopQueueSyncer()
	end)
end
