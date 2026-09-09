SetupPlayers = function()
	Multi0 = Player.GetPlayer("Multi0")
	Multi1 = Player.GetPlayer("Multi1")
	Multi2 = Player.GetPlayer("Multi2")
	Multi3 = Player.GetPlayer("Multi3")
	Multi4 = Player.GetPlayer("Multi4")
	Multi5 = Player.GetPlayer("Multi5")
	ScrinRebels = Player.GetPlayer("ScrinRebels")
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	MaleficScrin = Player.GetPlayer("MaleficScrin")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { MaleficScrin, GDI }
	SinglePlayerPlayer = ScrinRebels
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)
	TransferMcvsToPlayers()

	Utils.Do(MissionPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	DeployExtraMcvs()
end

AfterTick = function()

end

DeployExtraMcvs = function()
	if BaseSharingEnabled then
		return
	end

	local mcvPlayers = GetMcvPlayers()

	if #mcvPlayers > 1 then
		local wormhole = Actor.Create("wormhole", true, { Owner = GetFirstActivePlayer(), Location = PlayerStart.Location })

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Utils.Do(mcvPlayers, function(p)
				if p ~= mcvPlayers[1] then
					local units = Reinforcements.Reinforce(p, { "smcv" }, { PlayerStart.Location }, 25, function(a) a.Scatter() end)
				end
			end)
		end)

		Trigger.AfterDelay(DateTime.Seconds(3), function()
			wormhole.Kill()
		end)
	end
end
