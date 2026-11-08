using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Collections.Generic;
using System.Linq;

namespace StardewRoguelike.Netcode
{
    public class NetChest : INetObject<NetFields>
    {
        public NetFields NetFields { get; }

        public readonly NetVector2 TileLocation = new();

        public readonly NetObjectList<Item> Items = new();

        public readonly NetBool IndeterminateItemChest = new();

        public readonly NetBool IsTimedChest = new();

        public readonly NetLong MustOpenBy = new();

        public bool Spawned { get; private set; } = false;

        public NetChest()
        {
            NetFields = new(NetFields.GetNameForInstance(this));
            // Claim ownership before registering anything: 1.6 names fields in
            // network error logs after their owner, and warns for every field added
            // without one.
            NetFields.SetOwner(this);
            InitNetFields();
        }

        public NetChest(Vector2 tileLocation, List<Item> items) : this()
        {
            TileLocation.Value = tileLocation;
            Items.CopyFrom(items);
        }

        public NetChest(Vector2 tileLocation) : this()
        {
            TileLocation.Value = tileLocation;
            IndeterminateItemChest.Value = true;
        }

        public NetChest(Vector2 tileLocation, long mustOpenBy) : this(tileLocation)
        {
            MustOpenBy.Value = mustOpenBy;
            IsTimedChest.Value = true;
        }

        public NetChest(Vector2 tileLocation, List<Item> items, long mustOpenBy) : this(tileLocation, items)
        {
            MustOpenBy.Value = mustOpenBy;
            IsTimedChest.Value = true;
        }

        protected void InitNetFields()
        {
            NetFields.AddField(TileLocation).AddField(Items).AddField(IndeterminateItemChest).AddField(IsTimedChest).AddField(MustOpenBy);
        }

        // Is only called for Game1.player
        public void Spawn(MineShaft mine)
        {
            if (Spawned)
                return;

            // A plain treasure floor makes an offer rather than rolling: the item
            // comes off the seed, so the exit a floor above could name it, and it
            // comes with a price.
            ChestFloor.Offer? offer = ChestFloor.IsChestFloor(mine) && IndeterminateItemChest.Value
                ? ChestFloor.OfferFor(Roguelike.GetLevelFromMineshaft(mine))
                : null;

            List<Item> items;
            if (offer is not null)
            {
                items = offer.Value.Item is null ? new() : new() { offer.Value.Item };
            }
            else if (IndeterminateItemChest.Value && Items.Count == 0)
            {
                items = new();
                List<string> playerItems = new();

                foreach (Item playerItem in Game1.player.Items)
                {
                    if (playerItem is MeleeWeapon || playerItem is Ring || playerItem is Boots)
                        playerItems.Add(playerItem.DisplayName);
                }

                if (Game1.player.boots.Value is not null)
                    playerItems.Add(Game1.player.boots.Value.DisplayName);
                if (Game1.player.leftRing.Value is not null)
                    playerItems.Add(Game1.player.leftRing.Value.DisplayName);
                if (Game1.player.rightRing.Value is not null)
                    playerItems.Add(Game1.player.rightRing.Value.DisplayName);

                Item item = Merchant.GetChestMerchantFloor(Roguelike.GetLevelFromMineshaft(mine), Game1.random)
                    .PickAnyRandomAvoiding(playerItems);
                items.Add(item);
            }
            else
                items = Items.ToList();

            Chest chest;
            if (offer is not null)
            {
                chest = new PricedChest(items, TileLocation.Value, offer.Value.Price, offer.Value.Gold)
                {
                    Tint = Color.White
                };
            }
            else if (IsTimedChest.Value)
            {
                chest = new TimedChest(MustOpenBy.Value, items, TileLocation.Value)
                {
                    Tint = Color.White
                };
            }
            else
            {
                chest = new(items, TileLocation.Value)
                {
                    Tint = Color.White
                };
            }

            mine.overlayObjects[TileLocation.Value] = chest;

            Spawned = true;
        }
    }
}
