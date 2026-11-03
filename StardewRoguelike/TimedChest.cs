using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace StardewRoguelike
{
    internal class TimedChest : Chest
    {
        private NetLong MustOpenBy { get; } = new();

        private bool locked { get; set; } = false;

        public long SecondsLeft { get; set; }

        public TimedChest() : base()
        {
            initNetFields();
        }

        public TimedChest(long mustOpenBy, List<Item> items, Vector2 tileLocation) : base(items, tileLocation)
        {
            MustOpenBy.Value = mustOpenBy;
            SecondsLeft = MustOpenBy.Value - ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            initNetFields();
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            NetFields.AddField(MustOpenBy);
        }

        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            if (locked)
                return false;

            return base.checkForAction(who, justCheckingForActivity);
        }

        public override void updateWhenCurrentLocation(GameTime time)
        {
            base.updateWhenCurrentLocation(time);

            if (SecondsLeft < 0 && !locked)
                locked = true;

            SecondsLeft = MustOpenBy.Value - ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        }
    }
}
