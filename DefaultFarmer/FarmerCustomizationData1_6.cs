using Microsoft.Xna.Framework;
using StardewValley;

namespace DefaultFarmer
{
    public class FarmerCustomizationData1_6
    {
        public string Name { get; set; } = "";

        public string FarmName { get; set; } = "";

        public string FavThing { get; set; } = "";

        public bool Gender { get; set; } = Game1.player.IsMale;

        public Color EyeColor { get; set; } = Game1.player.newEyeColor.Value;

        public Color HairColor { get; set; } = Game1.player.hairstyleColor.Value;

        public Color PantsColor { get; set; } = Game1.player.pantsColor.Value;

        public int Skin { get; set; } = Game1.player.skin.Value;

        public int Hair { get; set; } = Game1.player.hair.Value;

        public string Shirt { get; set; } = Game1.player.shirt.Value;

        public string Pants { get; set; } = Game1.player.pants.Value;

        public int Accessory { get; set; } = Game1.player.accessory.Value;

        public string WhichPetType { get; set; } = Game1.player.whichPetType;

        public string WhichPetBreed { get; set; } = Game1.player.whichPetBreed;

        public bool SkipIntro { get; set; } = false;
    }
}
