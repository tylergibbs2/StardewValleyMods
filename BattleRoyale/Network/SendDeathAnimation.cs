using StardewValley;
using System.Collections.Generic;

namespace BattleRoyale.Network
{
    class SendDeathAnimation : NetworkMessage
    {
        public SendDeathAnimation()
        {
            MessageType = Utils.NetworkUtils.MessageTypes.SEND_DEATH_ANIMATION;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            Utils.FarmerUtils.PlayDeathAnimation(source);
        }
    }
}
