using HarmonyLib;
using System;
using Microsoft.Xna.Framework;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    // 1.6 split this into a raw-string overload and a tokenized one; the latter is
    // where the behaviour lives.
    [HarmonyPatch(typeof(GameLocation), "performTouchAction", new Type[] { typeof(string[]), typeof(Vector2) })]
    internal class GameLocationTouchActionPatch
    {
        public static void Postfix(GameLocation __instance, string[] action, Vector2 playerStandingPosition)
        {
            if (__instance is MineShaft mine && action.Length > 0 && action[0] == "DwarfSwitch")
            {
                Point tile_point = new((int)playerStandingPosition.X, (int)playerStandingPosition.Y);
                foreach (DwarfGate gate in mine.get_MineShaftDwarfGates())
                {
                    if (gate.switches.ContainsKey(tile_point) && !gate.switches[tile_point] && !gate.get_DwarfGateDisabled().Value)
                        gate.pressEvent.Fire(tile_point);
                }
            }
        }
    }
}
