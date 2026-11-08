using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Runs the keystone perks when a monster dies.
    /// </summary>
    /// <remarks>
    /// A postfix, and a patch of its own, because the prefix below returns false on
    /// boss and challenge floors to stop the ordinary drops - and a keystone has to
    /// go off wherever the monster died, not only where it would have dropped gold.
    /// </remarks>
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    internal class KeystoneKillPatch
    {
        public static void Postfix(GameLocation __instance, Monster monster, int x, int y)
        {
            Keystones.OnMonsterKilled(__instance, monster, x, y);
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.monsterDrop))]
    internal class MobDropHookPatch
    {
        public static bool Prefix(GameLocation __instance, Monster monster, int x, int y, Farmer who)
        {
            if (BossFloor.IsBossFloor((MineShaft)__instance) || ChallengeFloor.IsChallengeFloor((MineShaft)__instance))
                return false;

            List<int> objects = new();
            int goldToDrop = Game1.random.Next(Roguelike.GoldDropMin, Roguelike.GoldDropMax + 1);

            for (int i = 0; i < goldToDrop; i++)
                objects.Add(384);

            // The chance rises per level rather than the amount dropped.
            int scavenger = Perks.PerkLevel(Perks.PerkType.Scavenger);
            if (scavenger > 0 && Game1.random.NextDouble() < 0.05 * scavenger)
                objects.Add(874);

            Vector2 playerPosition = new(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y);

            for (int k = 0; k < objects.Count; k++)
            {
                int objectToAdd = objects[k];
                if (objectToAdd < 0)
                    __instance.debris.Add(monster.ModifyMonsterLoot(new Debris(Math.Abs(objectToAdd), Game1.random.Next(1, 4), new Vector2(x, y), playerPosition)));
                else
                    __instance.debris.Add(monster.ModifyMonsterLoot(new Debris(objectToAdd, new Vector2(x, y), playerPosition)));
            }

            return false;
        }
    }
}
