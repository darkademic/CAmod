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
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR, Scrin }
	SinglePlayerPlayer = ScrinRebels
	Utils.Do(MissionPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
end

AfterTick = function()

end

TransferNodBaseUnits = function(units)
	AssignToCoopPlayers(units)
	Utils.Do(units, function(a)
		if a.Type == "msg" then
			a.Undeploy()
		end
	end)
end

TransferStrandedNodUnits = function(units)
	Notification("Nod units located.")
	MediaCA.PlaySound(MissionDir .. "/s_nodunitslocated.aud", 2)
	AssignToCoopPlayers(units)
	Utils.Do(units, function(a)
		if a.Type == "msg" then
			a.Undeploy()
		end
	end)
end

TransferRebelStructures = function(structures)
	Notification("Rebel structures reclaimed.")
	MediaCA.PlaySound(MissionDir .. "/s_rebstrucreclaimed.aud", 2)
	local recipientPlayer = GetFirstActivePlayer()
	Utils.Do(structures, function(a)
		a.Owner = recipientPlayer
	end)
	Trigger.AfterDelay(1, function()
		Actor.Create("QueueUpdaterDummy", true, { Owner = recipientPlayer })
	end)
end
