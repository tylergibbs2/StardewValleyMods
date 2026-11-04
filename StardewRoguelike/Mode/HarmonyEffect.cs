namespace StardewRoguelike.Mode
{
    /// <summary>Applies and removes every mode-scoped Harmony patch.</summary>
    internal sealed class HarmonyEffect : IModeEffect
    {
        public string Name => "harmony";

        public void Activate() => Patch.PatchModeScoped();

        public void Deactivate() => Patch.UnpatchModeScoped();
    }
}
