using Microsoft.Xna.Framework;
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
            Rare
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
            Tiller           = 27
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
            PerkType.TurtleShell, PerkType.Leech
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

            _ => 3
        };

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
                _ => "Not Implemented"
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
                _ => "Not Implemented"
            };
        }

        public static Rectangle GetPerkSourceRect(PerkType perkType)
        {
            return new((int)perkType % 6 * 16, 624 + (int)perkType / 6 * 16, 16, 16);
        }

        public static PerkRarity GetPerkRarity(PerkType perkType)
        {
            if (CommonPerks.Contains(perkType))
                return PerkRarity.Common;
            else if (UncommonPerks.Contains(perkType))
                return PerkRarity.Uncommon;
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
            PerkType? first = null;
            PerkType? second = null;

            foreach (PerkType perk in Run.RunPlan.ShrineOrderFor(level))
            {
                if (!CanTake(perk))
                    continue;

                if (first is null)
                    first = perk;
                else
                {
                    second = perk;
                    break;
                }
            }

            return (first, second);
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
