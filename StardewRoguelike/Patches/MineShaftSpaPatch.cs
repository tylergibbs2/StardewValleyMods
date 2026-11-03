using HarmonyLib;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    internal class MineShaftSpaPatch
    {
        private static Texture2D swimShadow = null!;

        private static int swimShadowTimer;

        private static int swimShadowFrame;

        [HarmonyPatch(typeof(MineShaft), "resetLocalState")]
        internal class ResetLocalState
        {
            private static bool oldValue;

            public static bool Prefix(MineShaft __instance)
            {
                foreach (DwarfGate dwarfGate in __instance.get_MineShaftDwarfGates())
                    dwarfGate.ResetLocalState();

                swimShadow = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\swimShadow");

                oldValue = __instance.isMonsterArea;
                __instance.isMonsterArea = false;
                return true;
            }

            public static void Postfix(MineShaft __instance)
            {
                __instance.isMonsterArea = oldValue;

                __instance.removeTile(new(0, 0), "Buildings");

                bool foundAnyWater = false;
                __instance.waterTiles = new WaterTiles(new bool[__instance.map.Layers[0].LayerWidth, __instance.map.Layers[0].LayerHeight]);
                __instance.waterColor.Value = ((__instance.getMineArea() == 80) ? (Color.Red * 0.8f) : (new Color(50, 100, 200) * 0.5f));
                for (int y = 0; y < __instance.map.GetLayer("Buildings").LayerHeight; y++)
                {
                    for (int x = 0; x < __instance.map.GetLayer("Buildings").LayerWidth; x++)
                    {
                        if (__instance.doesTileHaveProperty(x, y, "Water", "Back") is not null || __instance.doesTileHaveProperty(x, y, "Water", "BackBack") is not null)
                        {
                            foundAnyWater = true;
                            __instance.waterTiles[x, y] = true;

                            if (__instance.getMineArea() == 80 && Game1.random.NextDouble() < 0.1)
                            {
                                string lightId = $"{ModEntry.ModId}_SpaWater_{x}_{y}";
                                __instance.sharedLights[lightId] = new LightSource(lightId, 4, new Vector2(x, y) * 64f, 2f, new Color(0, 220, 220), LightSource.LightContext.None, 0L);
                            }
                        }
                    }
                }

                if (!foundAnyWater)
                    __instance.waterTiles = null;
            }
        }

        [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.cleanupBeforePlayerExit))]
        internal class CleanupBeforePlayerExit
        {
            public static void Postfix(MineShaft __instance)
            {
                if (Game1.player.swimming.Value)
                    Game1.player.swimming.Value = false;
                if (Game1.locationRequest is not null)
                    Game1.player.bathingClothes.Value = false;

                __instance.unhook_getDebuffPlayerEvent();
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.draw))]
        internal class Draw
        {
            public static void Postfix(GameLocation __instance, SpriteBatch b)
            {
                if (__instance is not MineShaft)
                    return;

                foreach (Farmer f in __instance.farmers)
                {
                    if (f.swimming.Value)
                        b.Draw(swimShadow, Game1.GlobalToLocal(Game1.viewport, f.Position + new Vector2(0f, f.Sprite.SpriteHeight / 4 * 4)), new Rectangle(swimShadowFrame * 16, 0, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
                }
            }
        }

        [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.UpdateWhenCurrentLocation))]
        internal class UpdateWhenCurrentLocation
        {
            public static void Postfix(MineShaft __instance, GameTime time)
            {
                foreach (DwarfGate dwarfGate in __instance.get_MineShaftDwarfGates())
                    dwarfGate.UpdateWhenCurrentLocation(time, __instance);

                swimShadowTimer -= time.ElapsedGameTime.Milliseconds;
                if (swimShadowTimer <= 0)
                {
                    swimShadowTimer = 70;
                    swimShadowFrame++;
                    swimShadowFrame %= 10;
                }
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performTouchAction), new Type[] { typeof(string[]), typeof(Vector2) })]
        internal class PerformTouchAction
        {
            public static bool Prefix(GameLocation __instance, string[] action, Vector2 playerStandingPosition)
            {
                if (__instance is not MineShaft || action.Length == 0 || action[0] != "PoolEntrance")
                    return true;


                if (!Game1.player.swimming.Value)
                {
                    Game1.player.health = Game1.player.maxHealth;
                    Game1.player.stamina = Game1.player.MaxStamina;
                    Curse.DOTDamageToTick = 0;
                    Game1.player.swimTimer = 800;
                    Game1.player.swimming.Value = true;
                    Game1.player.changeIntoSwimsuit();
                    Game1.player.position.Y += 16f;
                    Game1.player.yVelocity = -8f;
                    __instance.playSound("pullItemFromWater");
                    RoguelikeUtility.MultiplayerInstance.broadcastSprites(__instance, new TemporaryAnimatedSprite(27, 100f, 4, 0, new Vector2(Game1.player.Position.X, Game1.player.StandingPixel.Y - 40), flicker: false, flipped: false)
                    {
                        layerDepth = 1f,
                        motion = new Vector2(0f, 2f)
                    });
                }
                else
                {
                    Game1.player.jump();
                    Game1.player.swimTimer = 800;
                    Game1.player.position.X = playerStandingPosition.X * 64f;
                    __instance.playSound("pullItemFromWater");
                    Game1.player.yVelocity = 8f;
                    Game1.player.swimming.Value = false;
                    Game1.player.changeOutOfSwimSuit();
                }
                Game1.player.noMovementPause = 500;

                return false;
            }
        }
    }
}
