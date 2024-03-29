﻿using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class TakeDamage : NetworkMessage
    {
        public TakeDamage()
        {
            MessageType = NetworkUtils.MessageTypes.TAKE_DAMAGE;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            DamageSource damageSource = (DamageSource)Convert.ToInt32(data[0]);
            int damage = Convert.ToInt32(data[1]);

            long? damagerID = null;
            string monster = "";
            if (damageSource == DamageSource.PLAYER && data.Count == 3 && data[2] != null)
                damagerID = Convert.ToInt64(data[2]);
            if (damageSource == DamageSource.MONSTER && data.Count == 3 && data[2] != null)
                monster = Convert.ToString(data[2]);

            FarmerUtils.TakeDamage(Game1.player, damageSource, damage, damagerID, monster);
        }
    }
}
