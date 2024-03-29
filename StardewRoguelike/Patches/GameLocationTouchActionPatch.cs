﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(GameLocation), "performTouchAction")]
    internal class GameLocationTouchActionPatch
    {
        public static void Postfix(GameLocation __instance, string fullActionString, Vector2 playerStandingPosition)
        {
            if (__instance is MineShaft mine && fullActionString.Split(' ')[0] == "DwarfSwitch")
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
