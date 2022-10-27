using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Xml.Serialization;

namespace JunimoBoy
{
    [XmlType("Mods_tylergibbs2_JunimoBoy")]
    public class JunimoBoy : Item
    {
        [XmlIgnore]
        public override string DisplayName
        {
            get { return I18n.JunimoBoy_ItemName(); }
            set { }
        }

        [XmlIgnore]
        public override int Stack
        {
            get { return 1; }
            set { }
        }

        public JunimoBoy() { }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            spriteBatch.Draw(ModEntry.ItemTextures, location + new Vector2(32f, 32f) * scaleSize, new(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f) * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
        }

        public override string getDescription()
        {
            return I18n.JunimoBoy_ItemDesc();
        }

        public override int maximumStackSize()
        {
            return 1;
        }

        public override int addToStack(Item stack)
        {
            return 1;
        }

        public override bool isPlaceable()
        {
            return false;
        }

        public override Item getOne()
        {
            return new JunimoBoy();
        }
    }
}
