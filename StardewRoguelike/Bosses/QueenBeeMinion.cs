using Microsoft.Xna.Framework;
using StardewValley.Monsters;
using System;

namespace StardewRoguelike.Bosses
{
    public class QueenBeeMinion : Fly
    {
        private string textureName = "Characters\\Monsters\\Fly_dangerous";

        public QueenBeeMinion () { }

        public QueenBeeMinion(Vector2 position, float difficulty) : base(position, false)
        {
            setTileLocation(position);

            // See BigBugMinion: a swarm has to be answerable to be a swarm rather
            // than a timer.
            MaxHealth = (int)Math.Round(45 * difficulty);
            Health = MaxHealth;

            DamageToFarmer = (int)Math.Round(15 * difficulty);

            Sprite = new(textureName);
        }

        public override void reloadSprite(bool onlyAppearance = false)
        {
            Sprite = new(textureName);
            HideShadow = true;
        }
    }
}
