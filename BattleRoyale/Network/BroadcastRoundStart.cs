﻿using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class BroadcastRoundStart : NetworkMessage
    {
        public BroadcastRoundStart()
        {
            MessageType = NetworkUtils.MessageTypes.SERVER_BROADCAST_ROUND_START;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            int numberOfPlayers = Convert.ToInt32(data[0]);
            int stormIndex = Convert.ToInt32(data[1]);

            int? specialRound = null;
            if (data.Count > 2)
                specialRound = Convert.ToInt32(data[2]);

            Round round;
            if (!Game1.IsServer)
                round = ModEntry.BRGame.CreateNewRound();
            else
                round = ModEntry.BRGame.GetActiveRound();

            if (specialRound != null)
                round.LoadClient(numberOfPlayers, stormIndex, (SpecialRoundType)specialRound);
            else
                round.LoadClient(numberOfPlayers, stormIndex, null);
        }
    }
}
