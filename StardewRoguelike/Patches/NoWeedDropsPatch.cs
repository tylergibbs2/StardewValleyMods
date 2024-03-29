using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Reflection;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(SObject), "cutWeed")]
    internal class NoWeedDropsPatch
    {
        public static bool Prefix(SObject __instance, Farmer who, GameLocation? location = null)
        {
            if (location is null && who is not null)
                location = who.currentLocation;
            if (location is null)
                return true;

			Color c = Color.Green;
			string sound = "cut";
			int animation = 50;
			__instance.Fragility = 2;

			Multiplayer multiplayer = (Multiplayer)typeof(Game1).GetField("multiplayer", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

			switch (__instance.ParentSheetIndex)
			{
				case 678:
					c = new Color(228, 109, 159);
					break;
				case 679:
					c = new Color(253, 191, 46);
					break;
				case 313:
				case 314:
				case 315:
					c = new Color(84, 101, 27);
					break;
				case 316:
				case 317:
				case 318:
					c = new Color(109, 49, 196);
					break;
				case 319:
					c = new Color(30, 216, 255);
					sound = "breakingGlass";
					animation = 47;
					location.playSound("drumkit2");
					break;
				case 320:
					c = new Color(175, 143, 255);
					sound = "breakingGlass";
					animation = 47;
					location.playSound("drumkit2");
					break;
				case 321:
					c = new Color(73, 255, 158);
					sound = "breakingGlass";
					animation = 47;
					location.playSound("drumkit2");
					break;
				case 792:
				case 793:
				case 794:
					break;
				case 882:
				case 883:
				case 884:
					c = new Color(30, 97, 68);
					break;
			}

			location.playSound(sound);
			multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(animation, __instance.TileLocation * 64f, c));
			multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(animation, __instance.TileLocation * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-48, 48)), c * 0.75f)
			{
				scale = 0.75f,
				flipped = true
			});

			multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(animation, __instance.TileLocation * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-48, 48)), c * 0.75f)
			{
				scale = 0.75f,
				delayBeforeAnimationStart = 50
			});

			multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(animation, __instance.TileLocation * 64f + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-48, 48)), c * 0.75f)
			{
				scale = 0.75f,
				flipped = true,
				delayBeforeAnimationStart = 100
			});

			if (!sound.Equals("breakingGlass"))
			{
				if (Game1.random.NextDouble() < 0.001)
					location.debris.Add(new Debris(new Hat(40), __instance.TileLocation * 64f + new Vector2(32f, 32f)));
			}

			if (Game1.random.NextDouble() < 0.02)
				location.addJumperFrog(__instance.TileLocation);

			return false;
        }
    }
}
