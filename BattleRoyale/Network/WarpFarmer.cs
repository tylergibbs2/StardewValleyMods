using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class WarpFarmer : NetworkMessage
    {
        public WarpFarmer()
        {
            MessageType = NetworkUtils.MessageTypes.WARP;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            string locationName = Convert.ToString(data[0]);
            int tileX = Convert.ToInt32(data[1]);
            int tileY = Convert.ToInt32(data[2]);

            Game1.player.FacingDirection = 2;
            Game1.player.warpFarmer(
                new TileLocation(locationName, tileX, tileY).CreateWarp()
            );
        }
    }
}
