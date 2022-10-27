using BattleRoyale.Patches;
using BattleRoyale.Utils;
using StardewValley;
using System;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class PlayerHitShakeTimer : NetworkMessage
    {
        public PlayerHitShakeTimer()
        {
            MessageType = NetworkUtils.MessageTypes.TELL_PLAYER_HIT_SHAKE_TIMER;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            int howLongMilliseconds = Convert.ToInt32(data[0]);

            source.temporarilyInvincible = true;
            source.temporaryInvincibilityTimer = 0;
            source.currentTemporaryInvincibilityDuration = howLongMilliseconds;

            if (source == Game1.player)
                Game1.hitShakeTimer = howLongMilliseconds;

            HitShaker.SetHitShakeTimer(source.UniqueMultiplayerID, howLongMilliseconds);
        }
    }
}
