﻿using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace BattleRoyale.Patches
{
    class DoorsAlwaysOpenPatch : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(GameLocation), "lockedDoorWarp");

        public static bool Prefix(GameLocation __instance, string[] actionParams)
        {
            Rumble.rumble(0.15f, 200f);
            Game1.player.completelyStopAnimatingOrDoingAction();
            __instance.playSoundAt("doorClose", Game1.player.getTileLocation());
            Game1.warpFarmer(actionParams[3], Convert.ToInt32(actionParams[1]), Convert.ToInt32(actionParams[2]), flip: false);

            return false;
        }
    }

    class InteriorDoorsAlwaysOpen : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(InteriorDoor), "Update");

        public static bool Prefix(InteriorDoor __instance, GameTime time)
        {
            if (__instance.Sprite != null)
            {
                ModEntry.BRGame.Helper.Reflection.GetMethod(__instance, "openDoorSprite").Invoke();
                ModEntry.BRGame.Helper.Reflection.GetMethod(__instance, "openDoorTiles").Invoke();

                __instance.Sprite.update(time);
            }

            return false;
        }
    }
}
