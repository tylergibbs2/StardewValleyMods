using StardewModdingAPI.Utilities;

namespace StardewNametags
{
    public class ModConfig
    {
        public bool MultiplayerOnly { get; set; } = false;
        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("F1");

        public string TextColor { get; set; } = "#FFFFFF";

        public string BackgroundColor { get; set; } = "#000000";

        public float BackgroundOpacity { get; set; } = 0.6f;

        public bool AlsoApplyOpacityToText { get; set; } = false;
    }
}
