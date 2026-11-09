using HarmonyLib;
using StardewRoguelike.Bosses;
using StardewValley;
using StardewValley.Tools;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Lets a swing reach the things on a floor that are not monsters.
    /// </summary>
    /// <remarks>
    /// Big Bug's eggs are four animated tiles on the Front layer rather than objects
    /// or terrain features, so nothing in the game's own tool handling can see them -
    /// performToolAction is never offered them, and damageMonster only ever looks at
    /// characters. This is the hook that gives the player a way to hit one.
    ///
    /// A postfix on DoDamage rather than on damageMonster, because damageMonster's
    /// result is about whether a monster was hurt: a swing that hits nothing but an
    /// egg has to count, and that swing never reaches damageMonster's success path.
    /// </remarks>
    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.DoDamage))]
    internal class MeleeWeaponDoDamagePatch
    {
        public static void Postfix(MeleeWeapon __instance, GameLocation location, Farmer who)
        {
            if (location is null || who != Game1.player)
                return;

            foreach (NPC character in location.characters)
            {
                if (character is BigBug bug)
                {
                    bug.TrySquashEgg(__instance.mostRecentArea);
                    break;
                }
            }
        }
    }
}
