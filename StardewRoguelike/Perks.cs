using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewRoguelike.UI;
using StardewValley;
using StardewValley.Buffs;
using System;
using System.Collections.Generic;

namespace StardewRoguelike
{
    public class Perks
    {
        public enum PerkRarity
        {
            Common,
            Uncommon,
            Rare,

            /// <summary>A perk that changes how the player fights, not by how much.</summary>
            Keystone
        }

        #pragma warning disable format
        public enum PerkType
        {
            Fighter          = 0,
            Scout            = 1,
            Gemologist       = 2,
            SpecialCharm     = 3,
            Reflect          = 4,
            Swift            = 5,
            Shield           = 6,
            Immunity         = 7,
            Precision        = 8,
            Discount         = 9,
            Miner            = 10,
            Brute            = 11,
            Defender         = 12,
            BarrelEnthusiast = 13,
            Leech            = 14,
            FoodEnjoyer      = 15,
            Acrobat          = 16,
            Desperado        = 17,
            RabbitsFoot      = 18,
            TurtleShell      = 19,
            Geologist        = 20,
            Scavenger        = 21,
            Sturdy           = 22,
            Indecisive       = 23,
            Fisherman        = 24,
            Deconstructor    = 25,
            Gamer            = 26,
            Tiller           = 27,

            // Keystones. These change what the player does rather than how much of
            // it they do, which is the thing the other twenty-eight never did: by
            // the fortieth floor a run was the same character swinging the same
            // sword about forty per cent harder.
            Volatile         = 28,
            Momentum         = 29,
            Spite            = 32,
            LastStand        = 33,
            Flurry           = 34,
            Prospector       = 35,

            // Fine perks, but not identities: a kill that shoots the next monster
            // and a kill that sometimes leaves a bomb are both more damage on a
            // kill, and the player does nothing differently for having them. They
            // sit with the other rare perks so that the pool a run draws its shape
            // from holds only things that shape a run.
            Ricochet         = 30,
            Bombardier       = 31
        }
        #pragma warning restore format

        public static readonly List<PerkType> CommonPerks = new()
        {
            PerkType.Fighter, PerkType.Scout, PerkType.Gemologist,
            PerkType.SpecialCharm, PerkType.Reflect, PerkType.Swift,
            PerkType.Shield, PerkType.Immunity, PerkType.Precision,
            PerkType.Discount, PerkType.Gamer, PerkType.Tiller
        };

        public static readonly List<PerkType> UncommonPerks = new()
        {
            PerkType.Miner, PerkType.Brute, PerkType.Defender,
            PerkType.BarrelEnthusiast, PerkType.FoodEnjoyer,
            PerkType.Geologist, PerkType.Scavenger, PerkType.Sturdy,
            PerkType.Indecisive, PerkType.Deconstructor, PerkType.Fisherman
        };

        public static readonly List<PerkType> RarePerks = new()
        {
            PerkType.Acrobat, PerkType.Desperado, PerkType.RabbitsFoot,
            PerkType.TurtleShell, PerkType.Leech,
            PerkType.Ricochet, PerkType.Bombardier
        };

        /// <summary>
        /// The perks that give a run its shape.
        ///
        /// A player holding none of these is offered them and nothing else, so every
        /// run picks up an identity at its first shrine and the rest of it is spent
        /// supporting that. After that they join the ordinary draw, rarely, so a
        /// second one is possible but not expected.
        /// </summary>
        /// <remarks>
        /// One for each way of playing, rather than one for each effect. They used to
        /// all go off on a kill, which meant all four wanted the same support - more
        /// damage, faster kills, more triggers - so four keystones were one archetype
        /// wearing four coats. These answer to different verbs, so each one makes a
        /// different part of the perk list worth having.
        /// </remarks>
        public static readonly List<PerkType> KeystonePerks = new()
        {
            PerkType.Volatile,    // on a kill
            PerkType.Momentum,    // on a run of kills
            PerkType.Spite,       // on being hit
            PerkType.LastStand,   // on being nearly dead
            PerkType.Flurry,      // on not being hit
            PerkType.Prospector   // on breaking rock
        };

        private static readonly PerScreen<PerkMenu?> ScreenCurrentMenu = new(() => null);

        /// <summary>The perk choice offered to the player on this screen.</summary>
        public static PerkMenu? CurrentMenu
        {
            get => ScreenCurrentMenu.Value;
            set => ScreenCurrentMenu.Value = value;
        }

        private static readonly int TotalPerkCount = CommonPerks.Count + UncommonPerks.Count + RarePerks.Count;

        private static readonly float CommonPerkChance = 0.6f;

        private static readonly float UncommonPerkChance = 0.3f;

        private static readonly float RarePerkChance = 0.1f;

        private static readonly PerScreen<Dictionary<PerkType, int>> ScreenActivePerks = new(() => new());

        /// <summary>
        /// The perks the player on this screen has taken.
        ///
        /// Per-screen, not static: in split-screen a plain static would hand both
        /// local players the same perk list, so one picking a perk would silently
        /// grant it to the other.
        /// </summary>
        /// Re-created on demand: StateEffect clears every PerScreen in the assembly
        /// by writing null into it when the mode turns off, which is the right thing
        /// for the perk list but leaves nothing to read from until the next run.
        private static Dictionary<PerkType, int> ActivePerks => ScreenActivePerks.Value ??= new();

        /// <summary>
        /// How many times a perk can be taken.
        ///
        /// A build is more of one thing, and until this existed there was no more of
        /// anything: every perk was taken once and then struck off the list, so the
        /// run collected breadth and could never commit to a direction.
        /// </summary>
        /// <summary>The health a single level of Defender is worth.</summary>
        private const int DefenderHealth = 25;

        public static int MaxLevel(PerkType perkType) => perkType switch
        {
            // Granted by handing the player a vanilla profession, which they either
            // hold or do not. There is no second copy of being a Fighter.
            PerkType.Fighter or PerkType.Scout or PerkType.Gemologist
                or PerkType.Miner or PerkType.Brute or PerkType.Acrobat
                or PerkType.Geologist or PerkType.Tiller => 1,

            // Permissions rather than quantities: there is no second helping of
            // being allowed to deconstruct an item.
            PerkType.Deconstructor or PerkType.Indecisive or PerkType.Gamer
                or PerkType.Fisherman or PerkType.Sturdy => 1,

            // Three, unless a keystone has a trait waiting deeper than that. Read off
            // the trait table rather than written down twice, so a trait cannot be
            // declared at a tier the perk does not reach.
            _ => Math.Max(DefaultMaxLevel, KeystoneTraits.MaxTier(perkType))
        };

        /// <summary>How deep a perk goes when nothing deeper is declared for it.</summary>
        private const int DefaultMaxLevel = 3;

        /// <summary>How many times the player has taken <paramref name="perkType"/>.</summary>
        public static int PerkLevel(PerkType perkType)
        {
            return ActivePerks.TryGetValue(perkType, out int level) ? level : 0;
        }

        /// <summary>Whether there is another level of this perk left to take.</summary>
        public static bool CanTake(PerkType perkType)
        {
            return PerkLevel(perkType) < MaxLevel(perkType);
        }

        /// <summary>
        /// A perk's name as an offer, which says where taking it would leave it.
        /// </summary>
        /// <remarks>
        /// A shrine offering a perk the player already carries is offering to deepen
        /// it, and the name alone does not say so: "Shield" and "Shield again" read
        /// identically. The level it would reach is written after it.
        /// </remarks>
        public static string GetOfferDisplayName(PerkType perkType)
        {
            int next = PerkLevel(perkType) + 1;

            return next > 1
                ? $"{GetPerkDisplayName(perkType)} {Numeral(next)}"
                : GetPerkDisplayName(perkType);
        }

        /// <summary>A perk level as a numeral, for the end of its name.</summary>
        public static string Numeral(int level) => level switch
        {
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => level.ToString()
        };

        public static string GetPerkDisplayName(PerkType perkType)
        {
            return perkType switch
            {
                PerkType.Fighter => I18n.PerkTypeName_Fighter(),
                PerkType.Scout => I18n.PerkTypeName_Scout(),
                PerkType.Gemologist => I18n.PerkTypeName_Gemologist(),
                PerkType.SpecialCharm => I18n.PerkTypeName_SpecialCharm(),
                PerkType.Reflect => I18n.PerkTypeName_Reflect(),
                PerkType.Swift => I18n.PerkTypeName_Swift(),
                PerkType.Shield => I18n.PerkTypeName_Shield(),
                PerkType.Immunity => I18n.PerkTypeName_Immunity(),
                PerkType.Precision => I18n.PerkTypeName_Precision(),
                PerkType.Discount => I18n.PerkTypeName_Discount(),
                PerkType.Miner => I18n.PerkTypeName_Miner(),
                PerkType.Brute => I18n.PerkTypeName_Brute(),
                PerkType.Defender => I18n.PerkTypeName_Defender(),
                PerkType.BarrelEnthusiast => I18n.PerkTypeName_BarrelEnthusiast(),
                PerkType.Leech => I18n.PerkTypeName_Leech(),
                PerkType.FoodEnjoyer => I18n.PerkTypeName_FoodEnjoyer(),
                PerkType.Acrobat => I18n.PerkTypeName_Acrobat(),
                PerkType.Desperado => I18n.PerkTypeName_Desperado(),
                PerkType.RabbitsFoot => I18n.PerkTypeName_RabbitsFoot(),
                PerkType.TurtleShell => I18n.PerkTypeName_TurtleShell(),
                PerkType.Geologist => I18n.PerkTypeName_Geologist(),
                PerkType.Scavenger => I18n.PerkTypeName_Scavenger(),
                PerkType.Sturdy => I18n.PerkTypeName_Sturdy(),
                PerkType.Indecisive => I18n.PerkTypeName_Indecisive(),
                PerkType.Fisherman => I18n.PerkTypeName_Fisherman(),
                PerkType.Deconstructor => I18n.PerkTypeName_Deconstructor(),
                PerkType.Gamer => I18n.PerkTypeName_Gamer(),
                PerkType.Tiller => I18n.PerkTypeName_Tiller(),
                PerkType.Spite => I18n.PerkTypeName_Spite(),
                PerkType.LastStand => I18n.PerkTypeName_LastStand(),
                PerkType.Flurry => I18n.PerkTypeName_Flurry(),
                PerkType.Prospector => I18n.PerkTypeName_Prospector(),
                PerkType.Volatile => I18n.PerkTypeName_Volatile(),
                PerkType.Momentum => I18n.PerkTypeName_Momentum(),
                PerkType.Ricochet => I18n.PerkTypeName_Ricochet(),
                PerkType.Bombardier => I18n.PerkTypeName_Bombardier(),
                _ => "Not Implemented"
            };
        }

        /// <summary>What a perk does, at the tier the caller is showing.</summary>
        /// <remarks>
        /// A keystone with traits does something different at each tier, so one
        /// description for all of them would describe the first and silently omit the
        /// rest. Asked for a tier that grants a trait, this answers with what that
        /// tier grants - which is what a shrine needs to say about the level it is
        /// offering, and what the perk list needs to say about the level that is held.
        ///
        /// It replaces the plain description rather than being added to it. Both
        /// menus draw this as a single line, so a tier list would need its own layout
        /// in two places to be worth having; what the player is deciding about is the
        /// tier in front of them.
        /// </remarks>
        public static string GetPerkDescription(PerkType perkType, int atLevel)
        {
            if (TierDescription(perkType, atLevel) is string tiered)
                return tiered;

            return GetPerkDescription(perkType);
        }

        /// <summary>What one tier of a keystone grants, or null if it grants nothing new.</summary>
        /// <remarks>
        /// Written out per tier rather than built up from the trait names in
        /// <see cref="KeystoneTraits"/>. A tier can grant two traits at once -
        /// Prospector's fourth grants both a wider burst and boulders - and two
        /// fragments stitched together read like a changelog rather than a sentence.
        /// <see cref="KeystoneTraits.GrantsAnythingAt"/> is the check that keeps the
        /// two in step.
        /// </remarks>
        private static string? TierDescription(PerkType perkType, int atLevel)
        {
            if (!KeystoneTraits.GrantsAnythingAt(perkType, atLevel))
                return null;

            return (perkType, atLevel) switch
            {
                (PerkType.Prospector, 2) => I18n.PerkTierDesc_Prospector2(),
                (PerkType.Prospector, 3) => I18n.PerkTierDesc_Prospector3(),
                (PerkType.Prospector, 4) => I18n.PerkTierDesc_Prospector4(),

                (PerkType.Volatile, 2) => I18n.PerkTierDesc_Volatile2(),
                (PerkType.Volatile, 3) => I18n.PerkTierDesc_Volatile3(),
                (PerkType.Volatile, 4) => I18n.PerkTierDesc_Volatile4(),

                (PerkType.Momentum, 2) => I18n.PerkTierDesc_Momentum2(),
                (PerkType.Momentum, 3) => I18n.PerkTierDesc_Momentum3(),
                (PerkType.Momentum, 4) => I18n.PerkTierDesc_Momentum4(),

                (PerkType.Spite, 2) => I18n.PerkTierDesc_Spite2(),
                (PerkType.Spite, 3) => I18n.PerkTierDesc_Spite3(),
                (PerkType.Spite, 4) => I18n.PerkTierDesc_Spite4(),

                (PerkType.LastStand, 2) => I18n.PerkTierDesc_LastStand2(),
                (PerkType.LastStand, 3) => I18n.PerkTierDesc_LastStand3(),
                (PerkType.LastStand, 4) => I18n.PerkTierDesc_LastStand4(),

                (PerkType.Flurry, 2) => I18n.PerkTierDesc_Flurry2(),
                (PerkType.Flurry, 3) => I18n.PerkTierDesc_Flurry3(),
                (PerkType.Flurry, 4) => I18n.PerkTierDesc_Flurry4(),

                _ => null
            };
        }

        public static string GetPerkDescription(PerkType perkType)
        {
            return perkType switch
            {
                PerkType.Fighter => I18n.PerkTypeDesc_Fighter(),
                PerkType.Scout => I18n.PerkTypeDesc_Scout(),
                PerkType.Gemologist => I18n.PerkTypeDesc_Gemologist(),
                PerkType.SpecialCharm => I18n.PerkTypeDesc_SpecialCharm(),
                PerkType.Reflect => I18n.PerkTypeDesc_Reflect(),
                PerkType.Swift => I18n.PerkTypeDesc_Swift(),
                PerkType.Shield => I18n.PerkTypeDesc_Shield(),
                PerkType.Immunity => I18n.PerkTypeDesc_Immunity(),
                PerkType.Precision => I18n.PerkTypeDesc_Precision(),
                PerkType.Discount => I18n.PerkTypeDesc_Discount(),
                PerkType.Miner => I18n.PerkTypeDesc_Miner(),
                PerkType.Brute => I18n.PerkTypeDesc_Brute(),
                PerkType.Defender => I18n.PerkTypeDesc_Defender(),
                PerkType.BarrelEnthusiast => I18n.PerkTypeDesc_BarrelEnthusiast(),
                PerkType.Leech => I18n.PerkTypeDesc_Leech(),
                PerkType.FoodEnjoyer => I18n.PerkTypeDesc_FoodEnjoyer(),
                PerkType.Acrobat => I18n.PerkTypeDesc_Acrobat(),
                PerkType.Desperado => I18n.PerkTypeDesc_Desperado(),
                PerkType.RabbitsFoot => I18n.PerkTypeDesc_RabbitsFoot(),
                PerkType.TurtleShell => I18n.PerkTypeDesc_TurtleShell(),
                PerkType.Geologist => I18n.PerkTypeDesc_Geologist(),
                PerkType.Scavenger => I18n.PerkTypeDesc_Scavenger(),
                PerkType.Sturdy => I18n.PerkTypeDesc_Sturdy(),
                PerkType.Indecisive => I18n.PerkTypeDesc_Indecisive(),
                PerkType.Fisherman => I18n.PerkTypeDesc_Fisherman(),
                PerkType.Deconstructor => I18n.PerkTypeDesc_Deconstructor(),
                PerkType.Gamer => I18n.PerkTypeDesc_Gamer(),
                PerkType.Tiller => I18n.PerkTypeDesc_Tiller(),
                PerkType.Spite => I18n.PerkTypeDesc_Spite(),
                PerkType.LastStand => I18n.PerkTypeDesc_LastStand(),
                PerkType.Flurry => I18n.PerkTypeDesc_Flurry(),
                PerkType.Prospector => I18n.PerkTypeDesc_Prospector(),
                PerkType.Volatile => I18n.PerkTypeDesc_Volatile(),
                PerkType.Momentum => I18n.PerkTypeDesc_Momentum(),
                PerkType.Ricochet => I18n.PerkTypeDesc_Ricochet(),
                PerkType.Bombardier => I18n.PerkTypeDesc_Bombardier(),
                _ => "Not Implemented"
            };
        }

        /// <summary>
        /// The sheet a perk's icon comes from.
        ///
        /// The mod's own icon grid holds exactly the twenty-eight perks it was drawn
        /// for and has nothing after them, so the keystones take an object off the
        /// game's sheet rather than an empty square.
        /// </summary>
        public static Texture2D GetPerkIconTexture(PerkType perkType)
        {
            // Flurry is the perk that wants a dagger in your hand, so it is drawn as
            // one. Nothing on the object sheet says "a run of quick blows" - it was a
            // spicy eel, which says dinner.
            if (perkType == PerkType.Flurry)
                return Game1.content.Load<Texture2D>(WeaponsTexture);

            return KeystoneIcon(perkType) is null ? Game1.mouseCursors : Game1.objectSpriteSheet;
        }

        /// <summary>The weapon sheet, for the one perk that is about a weapon.</summary>
        private const string WeaponsTexture = "TileSheets\\weapons";

        /// <summary>The blade Flurry is drawn as.</summary>
        private const int CrystalDagger = 21;

        public static Rectangle GetPerkSourceRect(PerkType perkType)
        {
            if (perkType == PerkType.Flurry)
            {
                return Game1.getSourceRectForStandardTileSheet(
                    GetPerkIconTexture(perkType), CrystalDagger, 16, 16);
            }

            if (KeystoneIcon(perkType) is int index)
                return Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, index, 16, 16);

            return new((int)perkType % 6 * 16, 624 + (int)perkType / 6 * 16, 16, 16);
        }

        /// <summary>The object a keystone borrows its icon from, if it is one.</summary>
        private static int? KeystoneIcon(PerkType perkType) => perkType switch
        {
            PerkType.Volatile => 286,    // cherry bomb
            PerkType.Momentum => 253,    // triple shot espresso
            PerkType.Spite => 769,       // void essence
            PerkType.LastStand => 881,   // bone fragment
            PerkType.Prospector => 386,  // iridium ore
            PerkType.Ricochet => 441,    // explosive ammo
            PerkType.Bombardier => 287,  // bomb
            _ => null
        };

        public static PerkRarity GetPerkRarity(PerkType perkType)
        {
            if (CommonPerks.Contains(perkType))
                return PerkRarity.Common;
            else if (UncommonPerks.Contains(perkType))
                return PerkRarity.Uncommon;
            else if (KeystonePerks.Contains(perkType))
                return PerkRarity.Keystone;
            else
                return PerkRarity.Rare;
        }

        /// <summary>The perks the player holds, with how deep each one goes.</summary>
        public static IReadOnlyDictionary<PerkType, int> GetActivePerks()
        {
            return ActivePerks;
        }

        public static PerkType GetRandomPerk()
        {
            double roll = Roguelike.OfferRng.NextDouble();
            if (roll < CommonPerkChance)
                return CommonPerks[Roguelike.OfferRng.Next(CommonPerks.Count)];
            else if (roll < CommonPerkChance + UncommonPerkChance)
                return UncommonPerks[Roguelike.OfferRng.Next(UncommonPerks.Count)];
            else if (roll < CommonPerkChance + UncommonPerkChance + RarePerkChance)
                return RarePerks[Roguelike.OfferRng.Next(RarePerks.Count)];

            throw new Exception("doesnt add up to 100");
        }

        public static PerkType? GetRandomUniquePerk()
        {
            if (HasAllPerks())
                return null;

            PerkType perk = GetRandomPerk();
            while (!CanTake(perk))
                perk = GetRandomPerk();

            return perk;
        }

        /// <summary>
        /// The two perks a shrine on <paramref name="level"/> offers this player.
        /// </summary>
        /// <remarks>
        /// The order comes from the seed, so it is the same answer every time it is
        /// asked and the exit menu can name it before anyone has walked down there.
        /// Which two of that order the player gets depends on what they already have,
        /// which is theirs alone - two players at one shrine are owed different
        /// offers, and each sees their own.
        /// </remarks>
        public static (PerkType?, PerkType?) GetShrineOffer(int level)
        {
            PerkType[] order = Run.RunPlan.ShrineOrderFor(level);

            // A run with no keystone yet is offered nothing else. Four or five perks
            // out of a pool this size means a keystone would otherwise turn up about
            // once in several runs, and a player cannot steer towards something they
            // will probably never meet.
            if (!HasAnyKeystone())
            {
                PerkType? firstKey = null;
                PerkType? secondKey = null;

                foreach (PerkType perk in order)
                {
                    if (!KeystonePerks.Contains(perk) || !CanTake(perk))
                        continue;

                    if (firstKey is null)
                        firstKey = perk;
                    else
                    {
                        secondKey = perk;
                        break;
                    }
                }

                return (firstKey, secondKey);
            }

            // One perk the player does not have, and one they can take further.
            //
            // Both drawn from the order would be two they do not have far more often
            // than not - the offer is two perks out of thirty-two, so the chance of
            // it holding something they already carry is about a quarter. Over the
            // eight shrines a run allows, deepening happened by coincidence roughly
            // once, and a run came out wide whatever the player was trying to do.
            // Offering one of each makes going deeper a decision instead.
            PerkType? fresh = null;
            PerkType? deeper = null;

            foreach (PerkType perk in order)
            {
                if (!CanTake(perk))
                    continue;

                if (PerkLevel(perk) == 0)
                    fresh ??= perk;
                else
                    deeper ??= perk;

                if (fresh is not null && deeper is not null)
                    break;
            }

            // Nothing to deepen yet, or nothing new left: fall back to filling the
            // offer from whichever the order still has.
            if (fresh is null || deeper is null)
            {
                PerkType? only = fresh ?? deeper;
                PerkType? backup = null;

                foreach (PerkType perk in order)
                {
                    if (!CanTake(perk) || perk == only)
                        continue;

                    backup = perk;
                    break;
                }

                return (only, backup);
            }

            return (fresh, deeper);
        }

        /// <summary>Whether the player has taken any perk that shapes a run.</summary>
        public static bool HasAnyKeystone()
        {
            foreach (PerkType perk in KeystonePerks)
            {
                if (HasPerk(perk))
                    return true;
            }

            return false;
        }

        public static (PerkType?, PerkType?) GetTwoRandomUniquePerks()
        {
            if (HasAllPerks())
                return (null, null);
            else if (ActivePerks.Count + 1 == TotalPerkCount)
                return (GetRandomUniquePerk()!.Value, null);

            PerkType perk1 = GetRandomUniquePerk()!.Value;
            PerkType perk2;
            do
            {
                perk2 = GetRandomUniquePerk()!.Value;
            } while (perk2 == perk1);

            return (perk1, perk2);
        }

        public static void AddPerk(PerkType perkType)
        {
            if (!CanTake(perkType))
                return;

            // Noted here rather than at the shrine, because a perk can arrive from a
            // merchant or a debug command too, and the map should show all of them.
            RunLog.PerkTaken(perkType, PerkLevel(perkType) + 1);

            // The one-off half of a perk - a profession, a lump of health - happens
            // when it is first taken. Taking it again only deepens what it already
            // does, which the effects read back off the level.
            if (HasPerk(perkType))
            {
                ActivePerks[perkType]++;

                // The one perk whose deepening is a lump rather than a rate.
                if (perkType == PerkType.Defender)
                {
                    Roguelike.TrueMaxHP += DefenderHealth;
                    Game1.player.health = Roguelike.TrueMaxHP;
                }

                RefreshPerkBuff();
                return;
            }

            switch (perkType)
            {
                case PerkType.Fighter:
                    Game1.player.professions.Add(24);
                    break;
                case PerkType.Scout:
                    Game1.player.professions.Add(25);
                    break;
                case PerkType.Gemologist:
                    Game1.player.professions.Add(23);
                    break;
                case PerkType.Miner:
                    Game1.player.professions.Add(18);
                    break;
                case PerkType.Brute:
                    Game1.player.professions.Add(26);
                    break;
                case PerkType.Defender:
                    Roguelike.TrueMaxHP += DefenderHealth;
                    Game1.player.health = Roguelike.TrueMaxHP;
                    break;
                case PerkType.Acrobat:
                    Game1.player.professions.Add(28);
                    break;
                case PerkType.Geologist:
                    Game1.player.professions.Add(19);
                    break;
                case PerkType.Tiller:
                    Game1.player.professions.Add(1);
                    break;
            }

            ActivePerks[perkType] = 1;
            RefreshPerkBuff();
        }

        public static void RemovePerk(PerkType perkType)
        {
            if (!HasPerk(perkType))
                return;

            switch (perkType)
            {
                case PerkType.Fighter:
                    Game1.player.professions.Remove(24);
                    break;
                case PerkType.Scout:
                    Game1.player.professions.Remove(25);
                    break;
                case PerkType.Gemologist:
                    Game1.player.professions.Remove(23);
                    break;
                case PerkType.Miner:
                    Game1.player.professions.Remove(18);
                    break;
                case PerkType.Brute:
                    Game1.player.professions.Remove(26);
                    break;
                case PerkType.Defender:
                    Roguelike.TrueMaxHP = Math.Max(0, Roguelike.TrueMaxHP - DefenderHealth * PerkLevel(PerkType.Defender));
                    if (Curse.HasCurse(CurseType.GlassCannon))
                        Game1.player.health = Math.Min(Roguelike.TrueMaxHP / 2, Game1.player.health);
                    else
                        Game1.player.health = Math.Min(Roguelike.TrueMaxHP, Game1.player.health);
                    break;
                case PerkType.Acrobat:
                    Game1.player.professions.Remove(28);
                    break;
                case PerkType.Geologist:
                    Game1.player.professions.Remove(19);
                    break;
                case PerkType.Tiller:
                    Game1.player.professions.Remove(1);
                    break;
            }

            ActivePerks.Remove(perkType);
            RefreshPerkBuff();
        }

        /// <summary>
        /// The buff carrying the passive stats the active perks grant.
        /// </summary>
        private const string PerkBuffId = $"{ModEntry.ModId}_Perks";

        /// <summary>
        /// Rebuilds <see cref="PerkBuffId"/> from the currently active perks.
        ///
        /// 1.6 removed the settable <c>Farmer.addedLuckLevel</c>, <c>addedSpeed</c> and
        /// <c>immunity</c> fields that these perks used to adjust; passive stats now
        /// have to come from the buff manager. One invisible buff holds the lot so the
        /// perks don't each take up a slot in the buff bar.
        /// </summary>
        /// <summary>
        /// Re-asserts the perk buff if it has gone missing while perks are still
        /// active. The buff is derived state: it should always match
        /// <see cref="ActivePerks"/>, so it is cheaper to keep it true every tick
        /// than to rely on every path that touches buffs leaving it alone.
        /// </summary>
        public static void EnsurePerkBuff()
        {
            if (Game1.player is null)
                return;

            bool shouldHaveBuff = HasPerk(PerkType.SpecialCharm) || HasPerk(PerkType.RabbitsFoot)
                               || HasPerk(PerkType.Swift) || HasPerk(PerkType.Immunity);

            if (shouldHaveBuff != Game1.player.hasBuff(PerkBuffId))
                RefreshPerkBuff();
        }

        private static void RefreshPerkBuff()
        {
            BuffEffects effects = new();

            // Per level rather than per perk. A perk taken twice is twice the perk,
            // which is the whole point of being allowed to take it twice.
            effects.LuckLevel.Value += PerkLevel(PerkType.SpecialCharm);
            effects.LuckLevel.Value += PerkLevel(PerkType.RabbitsFoot) * 3;
            effects.Speed.Value += PerkLevel(PerkType.Swift);
            effects.Immunity.Value += PerkLevel(PerkType.Immunity) * 2;

            if (!effects.HasAnyValue())
            {
                Game1.player.buffs.Remove(PerkBuffId);
                return;
            }

            Game1.player.applyBuff(new Buff(
                id: PerkBuffId,
                source: "Roguelike",
                displaySource: "Roguelike",
                duration: Buff.ENDLESS,
                effects: effects
            )
            {
                visible = false
            });
        }

        public static bool HasPerk(PerkType perkType)
        {
            return ActivePerks.ContainsKey(perkType);
        }

        public static bool HasAllPerks()
        {
            foreach (PerkType perk in Enum.GetValues<PerkType>())
            {
                if (CanTake(perk))
                    return false;
            }

            return true;
        }

        public static void RemoveAllPerks()
        {
            foreach (PerkType perkType in Enum.GetValues(typeof(PerkType)))
                RemovePerk(perkType);
        }
    }
}
