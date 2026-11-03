using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewRoguelike.HatQuests;
using StardewRoguelike.VirtualProperties;
using StardewValley;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(GameLocation), "breakStone")]
    internal class GameLocationBreakStonePatch
    {
        public static void Postfix(GameLocation __instance, string stoneId, int x, int y, Farmer who, Random r)
        {
            // gold ore
            if (stoneId != "764" || who != Game1.player)
                return;

            if (Game1.player.get_FarmerActiveHatQuest() is not null)
                Game1.player.get_FarmerActiveHatQuest()!.GoldMined++;

            // Spawn extra gold for hard hat
            if (HatQuest.HasBuffFor(HatQuestType.HARD_HAT))
            {

                Game1.createMultipleObjectDebris("384", x, y, 2, who.UniqueMultiplayerID, __instance);
                RoguelikeUtility.MultiplayerInstance.broadcastSprites(__instance, Utility.sparkleWithinArea(new Rectangle(x * 64, (y - 1) * 64, 32, 96), 3, Color.Yellow * 0.5f, 175, 100));
            }
        }
    }
}
