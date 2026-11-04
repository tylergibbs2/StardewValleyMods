using HarmonyLib;
using StardewValley.Locations;
using System;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Teaches the game to recognise the roguelike's mine level names.
    ///
    /// The mod names its levels <c>UndergroundMine{floor}/{level}</c> so it can carry
    /// both numbers through a warp. In 1.5.6 nothing parsed that name beyond
    /// <c>MineShaft.GetMine</c>, which the mod patches.
    ///
    /// 1.6 added <see cref="MineShaft.IsGeneratedLevel(string, out int, out int?)"/> and
    /// made it the gate for whether a name is a mine level at all —
    /// <c>Game1.getLocationFromName</c> only reaches <c>GetMine</c> when it returns true,
    /// and roughly a dozen other places in Game1 use it to decide mine-specific music,
    /// lighting and warp behaviour. It parses the level with int.TryParse, so the slash
    /// makes it fail and the location can never be resolved: warping into the mine
    /// silently dropped the player back in the lobby.
    /// </summary>
    // level/forceLayout are out parameters, so the overload has to be named with
    // ArgumentType.Out; a plain Type[] describes a different (non-existent) overload.
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.IsGeneratedLevel),
        new Type[] { typeof(string), typeof(int), typeof(int?) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
    internal class IsGeneratedLevelPatch
    {
        private const string Prefix_ = "UndergroundMine";

        public static bool Prefix(string locationName, ref int level, ref int? forceLayout, ref bool __result)
        {
            if (locationName is null || !locationName.StartsWith(Prefix_, StringComparison.OrdinalIgnoreCase))
                return true;

            string raw = locationName[Prefix_.Length..];
            int slash = raw.IndexOf('/');
            if (slash < 0)
                return true;   // a vanilla name; let the game parse it

            if (!int.TryParse(raw[(slash + 1)..], out int parsedLevel))
                return true;

            // The part after the slash is the roguelike floor the rest of the game
            // should treat as the mine level.
            level = parsedLevel;
            forceLayout = null;
            __result = true;
            return false;
        }
    }
}
