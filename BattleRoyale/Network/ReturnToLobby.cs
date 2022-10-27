using BattleRoyale.Utils;
using StardewValley;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class ReturnToLobby : NetworkMessage
    {
        public ReturnToLobby()
        {
            MessageType = NetworkUtils.MessageTypes.RETURN_TO_LOBBY;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            ModEntry.BRGame.ReturnToLobby();
        }
    }
}
