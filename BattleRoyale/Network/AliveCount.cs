using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class AliveCount : NetworkMessage
    {
        public AliveCount()
        {
            MessageType = NetworkUtils.MessageTypes.BROADCAST_ALIVE_COUNT;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            Round round = ModEntry.BRGame.GetActiveRound();
            int howManyPlayersAlive = Convert.ToInt32(data[0]);

            if (round?.overlayUI != null)
                round.overlayUI.AlivePlayers = howManyPlayersAlive;
        }
    }
}
