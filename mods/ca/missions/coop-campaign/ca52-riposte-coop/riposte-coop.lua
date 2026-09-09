SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	HawthorneGDI = Player.GetPlayer("HawthorneGDI")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { HawthorneGDI }
	SinglePlayerPlayer = ScrinRebels
	ScrinRebelPlayers = Utils.Where({ Multi0, Multi2, Multi3, Multi5, Multi4 }, function(p) return p ~= nil end)
	NodPlayers = Utils.Where({ Multi1 }, function(p) return p ~= nil end)
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
	TransferMcvsToPlayers(ScrinRebelPlayers)
	AssignToCoopPlayers(GetSpreadableUnits(SinglePlayerPlayer), ScrinRebelPlayers)

	if #NodPlayers > 0 then
		local nodUnits = GetSpreadableUnits(Nod)
		AssignToCoopPlayers(nodUnits, NodPlayers)
		TransferBaseToPlayer(Nod, NodPlayers[1])
	end

	Utils.Do(ScrinRebelPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	Trigger.AfterDelay(1, function()
		StopSpread = false
	end)
end

AfterTick = function()

end
