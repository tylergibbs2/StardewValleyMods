using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;


namespace BattleRoyale.Network
{
    class ReceiveClientDeath : NetworkMessage
    {
        public ReceiveClientDeath()
        {
            MessageType = NetworkUtils.MessageTypes.ANNOUNCE_CLIENT_DEATH;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            if (!Game1.IsServer)
                return;

            DamageSource damageSource = (DamageSource)Convert.ToInt32(data[0]);
            string monster = Convert.ToString(data[1]);

            Farmer killer = null;
            if (data.Count > 2)
            {
                long killerId = Convert.ToInt64(data[2]);
                killer = Game1.getFarmer(killerId);
            }

            Round round = ModEntry.BRGame.GetActiveRound();
            if (round != null)
                round.HandleDeath(damageSource, source, killer, monster);
        }
    }
}
