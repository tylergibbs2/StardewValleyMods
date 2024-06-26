using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace DefaultFarmer
{
    public class PresetButton : ClickableComponent
    {
        public readonly float BaseScale = 1f;

        public float Scale = 1f;

        public Color TextColor = Game1.textColor;

        public bool IsSelected;

        public PresetButton(Rectangle bounds, string name, string label, bool isSelected) : base(bounds, name, label)
        {
            IsSelected = isSelected;
        }

        public void Draw(SpriteBatch b)
        {
            if (IsSelected)
            {
                Scale = Math.Min(Scale + 0.04f, BaseScale + 0.25f);
                TextColor = Game1.unselectedOptionColor;
            }
            else
            {
                Scale = Math.Max(Scale - 0.04f, BaseScale);
                TextColor = Game1.textColor;
            }

            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                CharacterCustomizationDefaults.textureBoxRect,
                bounds.X - 16,
                bounds.Y - 16,
                bounds.Width + 32,
                bounds.Height + 24,
                Color.White,
                drawShadow: false,
                scale: Scale
            );

            Utility.drawBoldText(
                b,
                label,
                Game1.smallFont,
                new Vector2(
                    bounds.X,
                    bounds.Y
                ),
                TextColor
            );
        }
    }

    public class CharacterCustomizationDefaults : CharacterCustomization
    {
        public static readonly int presetCount = 9;

        private readonly List<PresetButton> presetButtons = new();

        private int selectedPreset = 0;

        private float saveScale = 1f;
        private readonly float saveBaseScale = 1f;

        private Color saveButtonColor = Game1.textColor;
        private ClickableComponent saveButton;

        public static readonly Rectangle textureBoxRect = new(0, 256, 60, 60);

        public CharacterCustomizationDefaults(Clothing item) : base(item)
        {
            setUpPositions();
            LoadDefaults(selectedPreset);
        }

        public CharacterCustomizationDefaults(Source source) : base(source)
        {
            setUpPositions();
            LoadDefaults(selectedPreset);
        }

        public void SaveDefaults(int which)
        {
            ModEntry.SaveDefaults(this, which);
        }

        public void LoadDefaults(int which)
        {
            ModEntry.LoadDefaults(this, which);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            setUpPositions();
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            for (int i = 0; i < presetButtons.Count; i++)
            {
                if (i == selectedPreset)
                    continue;

                PresetButton button = presetButtons[i];

                if (button.containsPoint(x, y))
                {
                    button.Scale = Math.Min(button.Scale + 0.04f, button.BaseScale + 0.25f);
                    button.TextColor = Game1.unselectedOptionColor;
                }
                else
                {
                    button.Scale = Math.Max(button.Scale - 0.04f, button.BaseScale);
                    button.TextColor = Game1.textColor;
                }
            }

            if (saveButton.containsPoint(x, y))
            {
                saveScale = Math.Min(saveScale + 0.04f, saveBaseScale + 0.25f);
                saveButtonColor = Game1.unselectedOptionColor;
            }
            else
            {
                saveScale = Math.Max(saveScale - 0.04f, saveBaseScale);
                saveButtonColor = Game1.textColor;
            }
        }

        private void setUpPositions()
        {
            string saveText = "Save";
            Vector2 saveTextSize = Game1.smallFont.MeasureString(saveText);
            saveButton = new(new(portraitBox.X + (portraitBox.Width / 2) - (int)saveTextSize.X / 2, portraitBox.Y - 16, (int)saveTextSize.X, (int)saveTextSize.Y), "saveButton", saveText);

            presetButtons.Clear();

            int buttonHeight = yPositionOnScreen + spaceToClearTopBorder + 90;
            int buttonBiggestWidth = 0;
            for (int i = 0; i < presetCount; i++)
            {
                string presetText = (i + 1).ToString();
                Vector2 presetTextSize = Game1.smallFont.MeasureString(presetText);

                presetButtons.Add(new(
                    new(
                        xPositionOnScreen + spaceToClearSideBorder * 3 + 2,
                        buttonHeight,
                        (int)presetTextSize.X,
                        (int)presetTextSize.Y
                    ),
                    $"presetButton{i}",
                    presetText,
                    selectedPreset == i
                ));

                if ((int)presetTextSize.X > buttonBiggestWidth)
                    buttonBiggestWidth = (int)presetTextSize.X;

                buttonHeight += (int)presetTextSize.Y + 32;
            }

            foreach (PresetButton button in presetButtons)
                button.bounds.Width = buttonBiggestWidth;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            for (int i = 0; i < presetButtons.Count; i++)
            {
                if (presetButtons[i].containsPoint(x, y))
                {
                    if (playSound)
                        Game1.playSound("bigSelect");
                    LoadDefaults(i);
                    selectedPreset = i;
                    presetButtons[i].IsSelected = true;

                    for (int j = 0; j < presetButtons.Count; j++)
                    {
                        if (i == j)
                            continue;

                        presetButtons[j].IsSelected = false;
                    }

                    break;
                }
            }

            if (saveButton != null && saveButton.containsPoint(x, y))
            {
                if (playSound)
                    Game1.playSound("bigSelect");
                SaveDefaults(selectedPreset);
            }
        }

        public void DrawButtons(SpriteBatch b)
        {
            drawTextureBox(
                b,
                Game1.menuTexture,
                textureBoxRect,
                saveButton.bounds.X - 16,
                saveButton.bounds.Y - 16,
                saveButton.bounds.Width + 32,
                saveButton.bounds.Height + 24,
                Color.White,
                drawShadow: false,
                scale: saveScale
            );

            Utility.drawBoldText(
                b,
                saveButton.label,
                Game1.smallFont,
                new Vector2(
                    saveButton.bounds.X,
                    saveButton.bounds.Y
                ),
                saveButtonColor
            );

            foreach (PresetButton button in presetButtons)
                button.Draw(b);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            DrawButtons(b);
            drawMouse(b);
        }
    }
}
