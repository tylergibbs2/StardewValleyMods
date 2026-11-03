using HarmonyLib;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Tools;
using System.Collections.Generic;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Makes the Deconstructor pay out gold ore for almost anything, so it stays a
    /// useful way to turn junk into money during a run.
    ///
    /// 1.5.6 needed a full <c>performObjectDropInAction</c> prefix plus a patch on
    /// <c>Object.GetDeconstructorOutput</c>. In 1.6 machines are data-driven: the
    /// Deconstructor entry in <c>Data/Machines</c> dispatches to the static
    /// <see cref="StardewValley.Object.OutputDeconstructor"/>, which is the only
    /// thing the mod has to override. Vanilla handles the rest of the interaction.
    /// </summary>
    [HarmonyPatch(typeof(SObject), nameof(SObject.OutputDeconstructor))]
    internal class DeconstructorOutputPatch
    {
        /// <summary>
        /// Items the Deconstructor refuses, so it can't be used to mint gold out of
        /// its own output or out of the shop's cheap stock.
        /// </summary>
        public static readonly HashSet<string> InvalidItemIds = new()
        {
            "384",  // Gold Ore
            "39",   // Dark Sign
            "322"   // Wood Fence
        };

        public static bool Prefix(Item inputItem, ref Item? __result, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = null;

            if (inputItem is null || InvalidItemIds.Contains(inputItem.ItemId) || inputItem is Pickaxe)
                __result = null;
            else
                __result = ItemRegistry.Create("(O)384", 3);

            return false;
        }
    }
}
