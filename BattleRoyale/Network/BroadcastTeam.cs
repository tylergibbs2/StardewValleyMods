using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class BroadcastTeam : NetworkMessage
    {
        public BroadcastTeam()
        {
            MessageType = NetworkUtils.MessageTypes.BROADCAST_TEAM;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            string team = Convert.ToString(data[0]);
            DelayedAction.functionAfterDelay(() =>
            {
                FarmerUtils.SetTeam(team);
            }, 750);
        }
    }
}
