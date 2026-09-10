SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	USSR = Player.GetPlayer("USSR")
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { USSR, Nod }
	SinglePlayerPlayer = ScrinRebels
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
	TransferMcvsToPlayers()

	Utils.Do(MissionPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)
end

AfterTick = function()

end
