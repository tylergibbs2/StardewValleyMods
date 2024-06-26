﻿using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class SynchronizeTime : NetworkMessage
    {
        public SynchronizeTime()
        {
            MessageType = NetworkUtils.MessageTypes.SYNCHRONIZE_TIME;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            string season = Convert.ToString(data[0]);
            int time = Convert.ToInt32(data[1]);

            TimeUtils.SetTime(season, time);
        }
    }
}
