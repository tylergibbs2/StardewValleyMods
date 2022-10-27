using Microsoft.Xna.Framework;
using StardewValley.Locations;
using System.Collections.Generic;

namespace StardewRoguelike.ChallengeFloors
{
    internal class GambaChests : ChallengeBase
    {
        private static readonly List<Vector2> ChestTiles = new()
        {
            new(10, 11),
            new(14, 11),
            new(18, 11),
            new(10, 15),
            new(14, 15),
            new(18, 15),
            new(10, 19),
            new(14, 19),
            new(18, 19)
        };

        public GambaChests() : base() { }

        public override List<string> MapPaths => new() { "custom-chest" };

        public override bool ShouldSpawnLadder(MineShaft mine) => false;

        public override void Initialize(MineShaft mine)
        {
            base.Initialize(mine);
        }
    }
}
