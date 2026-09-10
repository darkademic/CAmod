
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	Greece = Player.GetPlayer("Greece")
	England = Player.GetPlayer("England")
	USSR = Player.GetPlayer("USSR")
	Civilians = Player.GetPlayer("Civilians")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { USSR }
	SinglePlayerPlayer = Greece
	CoopInit()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
