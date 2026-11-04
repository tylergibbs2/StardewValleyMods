using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewRoguelike.Mode;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Crops;

namespace StardewRoguelike
{
    internal static class ContentManager
    {

        /// <summary>
        /// All assets that have been modified.
        /// The dictionary key is the name of the asset as the game requests it,
        /// the value is the filepath of the asset to replace it with.
        /// </summary>
        /// <summary>
        /// The vanilla assets the roguelike replaces. The cache has to be dropped
        /// for each of these whenever the mode flips, in both directions.
        /// </summary>
        public static IEnumerable<string> ModeScopedAssets => ModifiedAssets.Keys.Concat(OverlaidAssets.Keys);

        /// <summary>
        /// Regions of vanilla sheets the mod draws its own art from.
        ///
        /// These used to be whole-sheet replacements, which meant shipping a copy of
        /// each vanilla sheet and overwriting everything on it. The copies were made
        /// against an older game version, so 1.6's art was being replaced with 1.5.6's:
        /// the mod's Cursors differed from the real one across 15% of the sheet, which
        /// is why the clock and money counter rendered wrong, and its Projectiles sheet
        /// was shorter than 1.6's and truncated the rows the game had added.
        ///
        /// Only the rectangles listed here are overlaid now, so every pixel the mod
        /// does not actually use stays whatever the game shipped.
        /// </summary>
        private static readonly Dictionary<string, (string FromPath, Rectangle[] Regions)> OverlaidAssets = new()
        {
            ["LooseSprites/Cursors"] = ("assets/TileSheets/Cursors.png", new[]
            {
                new Rectangle(0, 608, 96, 96),     // shop refresh icon, then the perk icons below it
                new Rectangle(340, 432, 65, 17),   // the run's money box, see DayTimeMoneyBoxDrawBoxPatch
                new Rectangle(128, 1184, 160, 112),// the gamba machine, see UI/GambaMenu
                new Rectangle(255, 1436, 12, 14)   // merchant backpack, see Merchant.Draw
            }),

            ["LooseSprites/ForgeMenu"] = ("assets/TileSheets/ForgeMenu.png", new[]
            {
                new Rectangle(136, 16, 24, 24)
            }),

            ["TileSheets/Projectiles"] = ("assets/TileSheets/Projectiles.png", new[]
            {
                new Rectangle(0, 0, 16, 16),       // ThunderKid's strike marker
                new Rectangle(48, 0, 16, 16)       // Modulosaurus' strike marker
            }),

            ["Maps/Festivals"] = ("assets/Maps/Festivals.png", new[]
            {
                new Rectangle(32, 32, 80, 16),     // egg hunt variants 66-70
                new Rectangle(464, 0, 32, 32),     // pick-a-path tiles 29, 30, 61, 62
                new Rectangle(320, 128, 16, 32),   // pick-a-path tiles 276, 308
                new Rectangle(400, 224, 32, 32)    // pick-a-path tiles 473, 474, 505, 506
            })
        };

        private static readonly Dictionary<string, string> ModifiedAssets = new()
        {
            { "Maps/Mine", "assets/Maps/custom-lobby.tmx" },
            { "TileSheets/HatBoard", "assets/TileSheets/HatBoard.png" },
            { "TerrainFeatures/CleansingCauldron", "assets/TileSheets/Cauldron.png" },
            { "TerrainFeatures/CleansingCauldronEmpty", "assets/TileSheets/CauldronEmpty.png" },
            { "TerrainFeatures/SpeedPad", "assets/TileSheets/speedrun_pads.png" }
        };

        private static readonly Dictionary<string, string> ModifiedStrings = new()
        {
            { "OptionsPage.cs.11281", "Access Perks" }
        };

        /// <summary>
        /// Price, edibility and description overrides for the objects the roguelike
        /// reuses. 1.6 replaced the slash-delimited Data/ObjectInformation asset with
        /// Data/Objects and its ObjectData model, so these are applied field by field.
        /// </summary>
        private static readonly Dictionary<string, (int Price, int? Edibility, string Description)> ModifiedObjects = new()
        {
            { "896", (2000, null, "Forge 3 of these into a Galaxy weapon to unleash its final form.") },
            { "472", (50,   null, "Takes 6 floors to mature.") },
            { "479", (100,  null, "Takes 12 floors to mature.") },
            { "490", (150,  null, "Takes 18 floors to mature.") },
            { "486", (200,  null, "Takes 24 floors to mature.") },
            { "347", (250,  null, "Takes 36 floors to mature.") },
            { "24",  (100,  10,   null) },
            { "254", (475,  45,   null) },
            { "276", (600,  -300, null) },
            { "268", (725,  50,   null) },
            { "417", (850,  -300, null) }
        };

        /// <summary>
        /// Crops are always base quality during a run.
        ///
        /// 1.5.6 needed a transpiler on Crop.harvest that zeroed a hardcoded local
        /// slot. 1.6 clamps harvested quality to the crop's HarvestMaxQuality from
        /// Data/Crops, so capping that is enough and the transpiler is gone.
        /// </summary>
        private const int ForcedCropQuality = 0;

        /// <summary>
        /// Custom audio files that get loaded into the game.
        /// </summary>
        /// The tuples in this list are in the format (name, category, filename, loop).
        private static readonly List<(string, string, string, bool)> CustomAudio = new()
        {
            ("bee_boss", "Music", "bee_boss.ogg", true),
            ("hold_your_ground", "Music", "hold_your_ground.ogg", true),
            ("jelly_junktion", "Music", "jelly_junktion.ogg", true),
            ("photophobia", "Music", "photophobia.ogg", true),
            ("gelus_defensor", "Music", "gelus_defensor.ogg", false),
            ("gelus_defensor_no_intro", "Music", "gelus_defensor_no_intro.ogg", true),
            ("fallenstar", "Sound", "star.ogg", false),
            ("ceaseless_and_incessant", "Music", "ceaseless_and_incessant.ogg", true),
            ("circus_freak", "Music", "circus_freak.ogg", true),
            ("invoke_the_ancient", "Music", "invoke_the_ancient.ogg", true)
        };

        /// <summary>
        /// Parses and loads the custom audio files into new cues and
        /// adds them to the game's soundbank.
        /// </summary>
        public static void SetupAudio()
        {
            foreach (var (name, category, filename, loop) in CustomAudio)
            {
                string filePath = Path.Combine(ModEntry.Instance.Helper.DirectoryPath, "assets", "Audio", filename);
                string extension = Path.GetExtension(filePath);
                SoundEffect audio = extension switch
                {
                    ".wav" => SoundEffect.FromFile(filePath),
                    ".ogg" => OggLoader.OpenOggFile(filePath),
                    _ => throw new NotImplementedException(),
                };
                CueDefinition cue = new(name, audio, Game1.audioEngine.GetCategoryIndex(category), loop)
                {
                    instanceLimit = 1,
                    limitBehavior = CueDefinition.LimitBehavior.ReplaceOldest
                };

                Game1.soundBank.AddCue(cue);
            }
        }

        /// <summary>
        /// Event handler for when a game asset is requested to be loaded.
        /// This handles loading all custom assets as defined in <see cref="ModifiedAssets"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="AssetRequestedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException">If a file attempting to be loaded has no implemented way to be loaded.</exception>
        public static void AssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // In a vanilla session the mod owns none of these. Maps/Mine is the one
            // that would be noticed first - it is the roguelike lobby - but the
            // cursor and forge sheets are just as wrong in someone else's save.
            if (!RoguelikeMode.IsActive)
                return;

            if (OverlaidAssets.TryGetValue(e.Name.BaseName, out var overlay))
            {
                e.Edit(
                    asset =>
                    {
                        IAssetDataForImage editor = asset.AsImage();
                        Texture2D source = ModEntry.Instance.Helper.ModContent.Load<Texture2D>(overlay.FromPath);

                        foreach (Rectangle region in overlay.Regions)
                            editor.PatchImage(source, region, region);
                    },
                    AssetEditPriority.Late);
                return;
            }

            if (ModifiedAssets.TryGetValue(e.Name.BaseName, out string? fromPath))
            {
                string extension = Path.GetExtension(fromPath);
                switch (extension)
                {
                    case ".tmx":
                        e.LoadFromModFile<xTile.Map>(fromPath, AssetLoadPriority.High);
                        return;
                    case ".png":
                        e.LoadFromModFile<Texture2D>(fromPath, AssetLoadPriority.High);
                        return;
                    default:
                        throw new NotImplementedException();
                }
            }

            // Fallback to load all custom maps
            if (e.Name.BaseName.StartsWith("Maps/"))
            {
                string map = e.Name.BaseName.Split("/").Last();
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $@"assets/Maps/{map}.tmx");
                if (File.Exists(path))
                {
                    e.LoadFromModFile<xTile.Map>($"assets/Maps/{map}.tmx", AssetLoadPriority.High);
                    return;
                }
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Strings\\StringsFromCSFiles"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> data = editor.AsDictionary<string, string>().Data;
                    foreach (var (key, value) in ModifiedStrings)
                        data[key] = value;
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data\\Objects"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, ObjectData> data = editor.AsDictionary<string, ObjectData>().Data;
                    foreach (var (key, (price, edibility, description)) in ModifiedObjects)
                    {
                        if (!data.TryGetValue(key, out ObjectData? entry))
                            continue;

                        entry.Price = price;
                        if (edibility.HasValue)
                            entry.Edibility = edibility.Value;
                        if (description is not null)
                            entry.Description = description;
                    }
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data\\Crops"))
            {
                e.Edit(editor =>
                {
                    foreach (CropData crop in editor.AsDictionary<string, CropData>().Data.Values)
                    {
                        crop.HarvestMinQuality = ForcedCropQuality;
                        crop.HarvestMaxQuality = ForcedCropQuality;
                    }
                });
            }
        }
    }
}
