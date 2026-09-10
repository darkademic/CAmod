
SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	GDI = Player.GetPlayer("GDI")
	USSR = Player.GetPlayer("USSR")
	HiddenGDI = Player.GetPlayer("HiddenGDI")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = GetActiveCoopPlayers({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 })
	MissionEnemies = { USSR }
	SinglePlayerPlayer = GDI
	CoopInit()
end

AfterWorldLoaded = function()

end

AfterTick = function()

end
