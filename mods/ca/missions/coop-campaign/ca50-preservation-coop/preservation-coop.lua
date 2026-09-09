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
	Nod = Player.GetPlayer("Nod")
	Neutral = Player.GetPlayer("Neutral")
	MissionPlayers = Utils.Where({ Multi0, Multi1, Multi2, Multi3, Multi4, Multi5 }, function(p) return p ~= nil end)
	MissionEnemies = { USSR, Scrin }
	SinglePlayerPlayer = ScrinRebels
	ScrinRebelPlayers = Utils.Where({ Multi0, Multi2, Multi3, Multi5 }, function(p) return p ~= nil end)
	NodPlayers = Utils.Where({ Multi1, Multi4 }, function(p) return p ~= nil end)
	StopSpread = true
	CoopInit()
end

AfterWorldLoaded = function()
	StartCashSpread(3500)

	Utils.Do(ScrinRebelPlayers, function(p)
		Actor.Create("rebel.allegiance", true, { Owner = p })
	end)

	local scrinRebelUnits = GetSpreadableUnits(SinglePlayerPlayer)
	AssignToCoopPlayers(scrinRebelUnits, ScrinRebelPlayers)

	local nodUnits = GetSpreadableUnits(Nod)
	if #NodPlayers > 0 then
		AssignToCoopPlayers(nodUnits, NodPlayers)
	end

	if BaseSharingEnabled then
		TransferBaseToPlayer(SinglePlayerPlayer, ScrinRebelPlayers[1])

		if #NodPlayers > 0 then
			TransferBaseToPlayer(Nod, NodPlayers[1])
		end
	else
		local centralBaseActors = Utils.Where(SinglePlayerPlayer.GetActors(), function(a)
			return IsBaseTransferActor(a) and a.Location.X > 80
		end)
		Utils.Do(centralBaseActors, function(a)
			a.Owner = ScrinRebelPlayers[1]
		end)

		local westBaseActors = Utils.Where(SinglePlayerPlayer.GetActors(), function(a)
			return IsBaseTransferActor(a) and a.Location.X < 80
		end)

		if #ScrinRebelPlayers > 1 then
			Utils.Do(westBaseActors, function(a)
				a.Owner = ScrinRebelPlayers[2]
			end)

			if #ScrinRebelPlayers > 2 then
				Actor.Create("cspk", true, { Owner = ScrinRebelPlayers[3], Location = CPos.New(126, 88) })
			end
		else
			Utils.Do(westBaseActors, function(a)
				a.Owner = ScrinRebelPlayers[1]
			end)
		end

		if #NodPlayers > 1 then
			Actor.Create("amcv", true, { Owner = NodPlayers[2], Location = CPos.New(211, 75), Facing = Angle.South })
		end

		CACoopQueueSyncer()
	end

	Trigger.AfterDelay(1, function()
		StopSpread = false
	end)
end

AfterTick = function()

end

AppendChargeStatus = function(text, chargePerc)
	return text
end
