﻿using Microsoft.Xna.Framework;
using StardewRoguelike.VirtualProperties;
using StardewValley;

namespace StardewRoguelike.Extensions
{
    public static class GameLocationExtensions
    {
        public static void DrawSpeechBubble(this GameLocation location, Vector2 drawPosition, string text, int duration)
        {
            var speechBubbles = location.get_SpeechBubbles();
            speechBubbles[drawPosition] = new(drawPosition, text, duration);
        }

        public static void debuffPlayers(this GameLocation location, Rectangle area, int debuff)
        {
            var debuffEvent = location.get_DebuffPlayerEvent();
            debuffEvent.Fire(area, debuff);
        }

        public static void performDebuffPlayers(this GameLocation location, Rectangle area, int debuff)
        {
            if (Game1.player.currentLocation == location && Game1.player.GetBoundingBox().Intersects(area))
                Game1.buffsDisplay.addOtherBuff(new Buff(debuff));
        }
    }
}
