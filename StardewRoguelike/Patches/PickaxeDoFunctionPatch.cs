using HarmonyLib;
using StardewValley;
using StardewValley.Tools;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Prospector <see cref="KeystoneTrait.FreeMining"/>: swinging a pickaxe is free.
    /// </summary>
    /// <remarks>
    /// Worth a tier of its own here in a way it would not be in the base game. Energy
    /// running out does not tire this player out, it kills them - Roguelike's update
    /// loop drops anyone below zero stamina to no health - so a floor of rock is a
    /// real risk to walk into and this tier is what takes the risk away rather than
    /// merely the inconvenience.
    ///
    /// The stamina is put back rather than the cost recalculated and skipped. What a
    /// swing costs is vanilla's arithmetic over upgrade level and mining level, and a
    /// patch that worked it out a second time would be a copy to keep in step with a
    /// formula that is not ours.
    /// </remarks>
    [HarmonyPatch(typeof(Pickaxe), nameof(Pickaxe.DoFunction))]
    internal class PickaxeDoFunctionPatch
    {
        public static void Prefix(Farmer who, out float __state)
        {
            __state = who?.Stamina ?? 0f;
        }

        public static void Postfix(Farmer who, float __state)
        {
            if (who is null || who != Game1.player)
                return;

            if (!KeystoneTraits.Has(KeystoneTrait.FreeMining))
                return;

            // Only ever a refund. Something that gave stamina back during the swing
            // would otherwise have it taken away again.
            if (who.Stamina < __state)
                who.Stamina = __state;
        }
    }
}
