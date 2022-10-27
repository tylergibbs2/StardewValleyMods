using BattleRoyale.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BattleRoyale.Network
{
    class BroadcastChatMessage : NetworkMessage
    {
        private readonly Regex colorRx = new(@"^\[(\w+)\]");
        public BroadcastChatMessage()
        {
            MessageType = NetworkUtils.MessageTypes.SERVER_BROADCAST_CHAT_MESSAGE;
        }

        public override void Receive(Farmer source, List<object> data)
        {
            string message = Convert.ToString(data[0]);

            string colorName = "white";
            message = colorRx.Replace(message, match =>
            {
                if (match.Groups.Count > 1)
                    colorName = match.Groups[1].Value;

                return "";
            });

            Color color = StardewValley.Menus.ChatMessage.getColorFromName(colorName);
            if (color == Color.White)
                color = Color.Gold;

            Game1.chatBox.addMessage(message, color);
        }
    }
}
