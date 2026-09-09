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
