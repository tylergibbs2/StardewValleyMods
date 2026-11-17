using HarmonyLib;
using StardewRoguelike.Mode;
using StardewRoguelike.UI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Offers the roguelike alongside the player's farms on the co-op host tab.
    /// </summary>
    /// <remarks>
    /// Always active, because the mode is off when somebody opens the co-op menu
    /// looking for a run to host - that is the whole reason hosting went missing.
    /// It adds a row rather than taking the farms away, so one tab offers both
    /// kinds of co-op and vanilla hosting still works with the mod installed.
    ///
    /// Patched on addSaveFiles rather than on FindSaveGames, which is where the
    /// mode-scoped list replacement sits: FindSaveGames is static and cannot tell
    /// which menu asked, while this is an instance method on LoadGameMenu that
    /// CoopMenu overrides. The ordinary load menu is left alone.
    /// </remarks>
    [AlwaysActive]
    [HarmonyPatch(typeof(CoopMenu), "addSaveFiles")]
    internal class CoopMenuHostSlot
    {
        public static void Postfix(CoopMenu __instance)
        {
            // While the mode is on the list is already the roguelike and nothing
            // else, so adding it again would offer it twice.
            if (RoguelikeMode.IsActive)
                return;

            Farmer? farmer = BundledSave.ReadFarmer();
            if (farmer is null)
                return;

            List<LoadGameMenu.MenuSlot> slots = (List<LoadGameMenu.MenuSlot>)typeof(CoopMenu)
                .GetField("hostSlots", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(__instance)!;

            // First, not last. The constructor has already put "host a new farm" in
            // and addSaveFiles has appended every farm behind it, so a row added on
            // the end sits below all of them - and below the fold entirely once
            // somebody has a few farms, which is no way to be found.
            slots.Insert(0, new RoguelikeHostSlot(__instance, farmer));

            // addSaveFiles calls this itself once it has filled the list, so a row
            // added afterwards needs it called again or it has no button to sit on.
            __instance.UpdateButtons();
        }
    }
}
