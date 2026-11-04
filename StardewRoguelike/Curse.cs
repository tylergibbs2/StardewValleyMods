using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Enchantments;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Enchantments;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewRoguelike
{
    /// <summary>
    /// All types of curses. The enum value is the identifier synced between
    /// players in <see cref="VirtualProperties.FarmerCurses"/>; the in-game
    /// buff ID is derived from the name by <see cref="Curse.GetBuffId"/>.
    /// </summary>
    #pragma warning disable format
    internal enum CurseType
    {
        GlassCannon           = 89000,
        DamageOverTime        = 89001,
        PlayerKnockback       = 89002,
        BrittleCrown          = 89003,
        MoreEnemiesLessHealth = 89004,
        //GestureOfTheDrowned   = 89005,
        BombsAroundPlayer     = 89006,
        DoubleSpeedEveryone   = 89007,
        HealOverTime          = 89008,
        BuffsMorePotent       = 89009,
        BootsBetterImmunity   = 89010,
        BootsBetterDefense    = 89011,
        MoreCritsLessDamage   = 89012,
        CheaperMerchant       = 89013,
        MonsterBuffs          = 89014
    }
    #pragma warning restore format

    internal class Curse : Buff
    {
        private static string GetCurseDisplayName(CurseType curseType)
        {
            return curseType switch
            {
                CurseType.GlassCannon => I18n.CurseTypeName_GlassCannon(),
                CurseType.DamageOverTime => I18n.CurseTypeName_DamageOverTime(),
                CurseType.PlayerKnockback => I18n.CurseTypeName_PlayerKnockback(),
                CurseType.BrittleCrown => I18n.CurseTypeName_BrittleCrown(),
                CurseType.MoreEnemiesLessHealth => I18n.CurseTypeName_MoreEnemiesLessHealth(),
                //CurseType.GestureOfTheDrowned => I18n.CurseTypeName_GestureOfTheDrowned()
                CurseType.BombsAroundPlayer => I18n.CurseTypeName_BombsAroundPlayer(),
                CurseType.DoubleSpeedEveryone => I18n.CurseTypeName_DoubleSpeedEveryone(),
                CurseType.HealOverTime => I18n.CurseTypeName_HealOverTime(),
                CurseType.BuffsMorePotent => I18n.CurseTypeName_BuffsMorePotent(),
                CurseType.BootsBetterImmunity => I18n.CurseTypeName_BootsBetterImmunity(),
                CurseType.BootsBetterDefense => I18n.CurseTypeName_BootsBetterDefense(),
                CurseType.MoreCritsLessDamage => I18n.CurseTypeName_MoreCritsLessDamage(),
                CurseType.CheaperMerchant => I18n.CurseTypeName_CheaperMerchant(),
                CurseType.MonsterBuffs => I18n.CurseTypeName_MonsterBuffs(),
                _ => throw new NotImplementedException(),
            };
        }

        private static string GetCurseDescription(CurseType curseType)
        {
            return curseType switch {
                CurseType.GlassCannon => I18n.CurseTypeDesc_GlassCannon(),
                CurseType.DamageOverTime => I18n.CurseTypeDesc_DamageOverTime(),
                CurseType.PlayerKnockback => I18n.CurseTypeDesc_PlayerKnockback(),
                CurseType.BrittleCrown => I18n.CurseTypeDesc_BrittleCrown(),
                CurseType.MoreEnemiesLessHealth => I18n.CurseTypeDesc_MoreEnemiesLessHealth(),
                //CurseType.GestureOfTheDrowned => I18n.CurseTypeDesc_GestureOfTheDrowned()
                CurseType.BombsAroundPlayer => I18n.CurseTypeDesc_BombsAroundPlayer(),
                CurseType.DoubleSpeedEveryone => I18n.CurseTypeDesc_DoubleSpeedEveryone(),
                CurseType.HealOverTime => I18n.CurseTypeDesc_HealOverTime(),
                CurseType.BuffsMorePotent => I18n.CurseTypeDesc_BuffsMorePotent(),
                CurseType.BootsBetterImmunity => I18n.CurseTypeDesc_BootsBetterImmunity(),
                CurseType.BootsBetterDefense => I18n.CurseTypeDesc_BootsBetterDefense(),
                CurseType.MoreCritsLessDamage => I18n.CurseTypeDesc_MoreCritsLessDamage(),
                CurseType.CheaperMerchant => I18n.CurseTypeDesc_CheaperMerchant(),
                CurseType.MonsterBuffs => I18n.CurseTypeDesc_MonsterBuffs(),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// The buff icon to use when displaying curses.
        /// </summary>
        public static readonly int IconId = 18;

        /// <summary>
        /// The duration (in milliseconds) of buffs that are made
        /// potent via the <see cref="CurseType.BuffsMorePotent"/> curse.
        /// </summary>
        public static readonly int PotentBuffDuration = 1000 * 60 * 5;

        /// <summary>
        /// The amount of game ticks to wait before spawning bombs when
        /// the player has the <see cref="CurseType.BombsAroundPlayer"/> curse.
        /// </summary>
        private static int TicksToSpawnBombs = 60 * 10;

        /// <summary>
        /// The amount of damage over time to deal when the player has the
        /// <see cref="CurseType.DamageOverTime"/> curse.
        /// </summary>
        public static int DOTDamageToTick = 0;

        /// <summary>
        /// The amount of heal over time to perform when the player has the
        /// <see cref="CurseType.HealOverTime"/> curse.
        /// </summary>
        public static int HOTHealToTick = 0;

        /// <summary>
        /// Gets the ID this curse is tracked under in <see cref="Farmer.buffs"/>.
        /// </summary>
        public static string GetBuffId(CurseType curseType)
        {
            return $"{ModEntry.ModId}_Curse_{curseType}";
        }

        /// <summary>
        /// Gets the stat changes a curse applies passively, if any. Everything
        /// else about a curse is implemented in code rather than as buff effects.
        /// </summary>
        private static BuffEffects GetCurseEffects(CurseType curseType)
        {
            BuffEffects effects = new();

            if (curseType == CurseType.DoubleSpeedEveryone)
                effects.Speed.Value = 1;

            return effects;
        }

        public Curse(CurseType curseType) : base(
            id: GetBuffId(curseType),
            source: "Roguelike",
            displaySource: "Roguelike",
            duration: ENDLESS,
            iconTexture: Game1.buffsIcons,
            iconSheetIndex: IconId,
            effects: GetCurseEffects(curseType),
            isDebuff: true,
            displayName: GetCurseDisplayName(curseType),
            description: GetCurseDescription(curseType))
        {
            CurseType = curseType;
        }

        /// <summary>
        /// Which curse this buff represents.
        /// </summary>
        public readonly CurseType CurseType;

        /// <summary>
        /// Spawns three cherry bombs within 3 tiles of the local player.
        /// </summary>
        public static void SpawnBombsAroundPlayer()
        {
            if (Game1.player.currentLocation is not MineShaft)
                return;

            MineShaft mine = (MineShaft)Game1.player.currentLocation;

            SObject bomb = new("286", 1);
            int bombsToSpawn = 3;
            int spawnPadding = 3;
            while (bombsToSpawn > 0)
            {
                Vector2 playerPosition = Game1.player.Tile;
                Vector2 tileToSpawn = new(
                    playerPosition.X + Game1.random.Next(-spawnPadding, spawnPadding),
                    playerPosition.Y + Game1.random.Next(-spawnPadding, spawnPadding)
                );
                mine.isTileClearForMineObjects(tileToSpawn);
                tileToSpawn *= 64f;

                bomb.placementAction(mine, (int)tileToSpawn.X, (int)tileToSpawn.Y, Game1.player);
                bombsToSpawn--;
            }
        }

        /// <summary>
        /// Adjusts a passed monster based on the curses
        /// that are active in the game.
        /// </summary>
        /// <param name="monster">The monster to adjust.</param>
        public static void AdjustMonster(ref Monster monster)
        {
            if (AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
            {
                monster.MaxHealth = (int)Math.Round(monster.MaxHealth * .75f);
                monster.Health = monster.MaxHealth;
            }
            if (AnyFarmerHasCurse(CurseType.DoubleSpeedEveryone))
                monster.Speed *= 2;
        }

        public static void UpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            bool isMonsterArea = Game1.player.currentLocation is MineShaft mine && mine.isMonsterArea;
            if (isMonsterArea && HasCurse(CurseType.BombsAroundPlayer) && Game1.shouldTimePass())
            {
                if (TicksToSpawnBombs > 0)
                {
                    TicksToSpawnBombs--;

                    if (TicksToSpawnBombs == 0)
                    {
                        SpawnBombsAroundPlayer();
                        TicksToSpawnBombs = Game1.random.Next(60 * 9, 60 * 12);
                    }
                }
            }

            //if (HasCurse(CurseType.GestureOfTheDrowned))
            //{
            //    if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is MeleeWeapon weapon && Game1.player.CanMove && !Game1.player.canOnlyWalk && !Game1.eventUp && !Game1.player.onBridge.Value)
            //        weapon.animateSpecialMove(Game1.player);
            //}

            if (!e.IsOneSecond)
                return;

            if (HasCurse(CurseType.DamageOverTime) && DOTDamageToTick > 0 && Game1.shouldTimePass())
            {
                int toRemove = Math.Max(3, DOTDamageToTick / 3);
                Game1.player.health -= toRemove;
                DOTDamageToTick = Math.Max(DOTDamageToTick - toRemove, 0);

                if (Game1.player.get_FarmerActiveHatQuest() is not null)
                    Game1.player.get_FarmerActiveHatQuest()!.DamageTaken += toRemove;

                if (Game1.player.health <= 0 && Game1.player.GetEffectsOfRingMultiplier("863") > 0 && !Game1.player.hasUsedDailyRevive.Value)
                {
                    Game1.player.startGlowing(new Color(255, 255, 0), border: false, 0.25f);
                    DelayedAction.functionAfterDelay(delegate
                    {
                        Game1.player.stopGlowing();
                    }, 500);
                    Game1.playSound("yoba");
                    for (int i = 0; i < 13; i++)
                    {
                        float xPos = Game1.random.Next(-32, 33);
                        Game1.player.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(114, 46, 2, 2), 200f, 5, 1, new Vector2(xPos + 32f, -96f), flicker: false, flipped: false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f)
                        {
                            attachedCharacter = Game1.player,
                            positionFollowsAttachedCharacter = true,
                            motion = new Vector2(xPos / 32f, -3f),
                            delayBeforeAnimationStart = i * 50,
                            alphaFade = 0.001f,
                            acceleration = new Vector2(0f, 0.1f)
                        });
                    }
                    Game1.player.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(157, 280, 28, 19), 2000f, 1, 1, new Vector2(-20f, -16f), flicker: false, flipped: false, 1E-06f, 0f, Color.White, 4f, 0f, 0f, 0f)
                    {
                        attachedCharacter = Game1.player,
                        positionFollowsAttachedCharacter = true,
                        alpha = 0.1f,
                        alphaFade = -0.01f,
                        alphaFadeFade = -0.00025f
                    });
                    Game1.player.health = (int)Math.Min(Game1.player.maxHealth, Game1.player.maxHealth * 0.5f + Game1.player.GetEffectsOfRingMultiplier("863"));
                    Game1.player.hasUsedDailyRevive.Value = true;
                    DOTDamageToTick = 0;
                }
            }

            if (HasCurse(CurseType.HealOverTime) && HOTHealToTick > 0 && Game1.shouldTimePass())
            {
                int toAdd = Math.Max(3, HOTHealToTick / 3);
                Game1.player.health = Math.Min(Game1.player.health + toAdd, Game1.player.maxHealth);
                HOTHealToTick = Math.Max(0, HOTHealToTick - toAdd);

                if (Game1.player.get_FarmerActiveHatQuest() is not null)
                    Game1.player.get_FarmerActiveHatQuest()!.HealthHealed += toAdd;
            }
        }

        /// <summary>
        /// The IDs 1.6 gives buffs that came from eating or drinking something.
        /// </summary>
        public static readonly string[] FoodBuffIds = new[] { "food", "drink" };

        /// <summary>
        /// Strengthens any food/drink buff the player already has, for when the
        /// <see cref="CurseType.BuffsMorePotent"/> curse is picked up while one is active.
        /// </summary>
        public static void MakeActiveFoodBuffsPotent()
        {
            foreach (string buffId in FoodBuffIds)
            {
                if (!Game1.player.buffs.AppliedBuffs.TryGetValue(buffId, out Buff? buff))
                    continue;

                MakePotent(buff);

                // Re-applying replaces the buff, which recalculates its effects.
                Game1.player.applyBuff(buff);
            }
        }

        /// <summary>
        /// Applies the <see cref="CurseType.BuffsMorePotent"/> curse to a buff by
        /// bumping each of its positive effects and shortening it to
        /// <see cref="PotentBuffDuration"/>.
        /// </summary>
        public static void MakePotent(Buff buff)
        {
            buff.millisecondsDuration = PotentBuffDuration;
            buff.totalMillisecondsDuration = PotentBuffDuration;

            // Only the additive stats; +1 on a multiplier would be +100%.
            NetFloat[] additive = new[]
            {
                buff.effects.CombatLevel, buff.effects.FarmingLevel, buff.effects.FishingLevel,
                buff.effects.MiningLevel, buff.effects.LuckLevel, buff.effects.ForagingLevel,
                buff.effects.MaxStamina, buff.effects.MagneticRadius, buff.effects.Speed,
                buff.effects.Defense, buff.effects.Attack, buff.effects.Immunity
            };

            foreach (NetFloat effect in additive)
            {
                if (effect.Value > 0)
                    effect.Value += 1;
            }
        }

        /// <summary>
        /// Re-applies the buff for any curse the player is supposed to have.
        ///
        /// The synced curse list is the source of truth; the buff is derived from it
        /// and <see cref="HasCurse"/> reads the buff, so a buff that went missing would
        /// silently switch the curse off while it still showed as taken.
        /// </summary>
        public static void EnsureCurseBuffs()
        {
            if (Game1.player is null)
                return;

            foreach (int curseValue in Game1.player.get_FarmerCurses())
            {
                CurseType curseType = (CurseType)curseValue;
                if (!Game1.player.hasBuff(GetBuffId(curseType)))
                    Game1.player.applyBuff(new Curse(curseType));
            }
        }

        // Only works for Game1.player
        public static bool HasCurse(CurseType curseType)
        {
            return Game1.player.hasBuff(GetBuffId(curseType));
        }

        public static bool HasAnyCurse()
        {
            return Game1.player.get_FarmerCurses().Count > 0;
        }

        public static bool HasAllCurses()
        {
            return Game1.player.get_FarmerCurses().Count == Enum.GetValues<CurseType>().Length;
        }

        public static bool AnyFarmerHasCurse(CurseType curseType)
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.get_FarmerCurses().Contains((int)curseType))
                    return true;
            }

            return false;
        }

        public static CurseType? GetRandomUniqueCurse(Random random = null)
        {
            random ??= Game1.random;

            List<CurseType> curseTypes = Enum.GetValues<CurseType>().ToList();
            curseTypes = curseTypes.Where(c => !HasCurse(c)).ToList();

            if (curseTypes.Count == 0)
                return null;

            return curseTypes[random.Next(curseTypes.Count)];
        }

        public static void RemoveRandomCurse()
        {
            int totalCurses = Game1.player.get_FarmerCurses().Count;
            if (totalCurses == 0)
                return;

            CurseType randomCurse = (CurseType)Game1.player.get_FarmerCurses().ElementAt(Game1.random.Next(totalCurses));
            RemoveCurse(randomCurse);
        }

        public static bool AddCurse(CurseType curseType)
        {
            if (HasCurse(curseType))
                return true;

            switch (curseType)
            {
                case CurseType.BootsBetterDefense:
                case CurseType.BootsBetterImmunity:
                    if (Game1.player.boots.Value != null)
                    {
                        Game1.player.boots.Value.reloadData();
                        Game1.player.boots.Value.onUnequip(Game1.player);
                    }
                    break;
                default:
                    break;
            }

            Curse curse = new(curseType);
            Game1.player.get_FarmerCurses().Add((int)curseType);
            Game1.player.applyBuff(curse);
            bool result = HasCurse(curseType);

            switch (curseType)
            {
                case CurseType.GlassCannon:
                    Game1.player.enchantments.Add(new GlassCannonEnchantment());
                    break;
                case CurseType.DamageOverTime:
                    DOTDamageToTick = 0;
                    break;
                case CurseType.HealOverTime:
                    HOTHealToTick = 0;
                    break;
                case CurseType.BootsBetterDefense:
                case CurseType.BootsBetterImmunity:
                    if (Game1.player.boots.Value != null)
                    {
                        Game1.player.boots.Value.reloadData();
                        Game1.player.boots.Value.onEquip(Game1.player);
                    }
                    break;
                case CurseType.BuffsMorePotent:
                    MakeActiveFoodBuffsPotent();
                    break;
                default:
                    break;
            }

            return result;
        }

        public static bool RemoveCurse(CurseType curseType)
        {
            if (!HasCurse(curseType))
                return true;

            switch (curseType)
            {
                case CurseType.GlassCannon:
                    GlassCannonEnchantment toRemove = null;
                    foreach (BaseEnchantment enchantment in Game1.player.enchantments)
                    {
                        if (enchantment is GlassCannonEnchantment glassCannon)
                        {
                            toRemove = glassCannon;
                            break;
                        }
                    }
                    if (toRemove is not null)
                        Game1.player.enchantments.Remove(toRemove);
                    break;
                case CurseType.DamageOverTime:
                    DOTDamageToTick = 0;
                    break;
                case CurseType.HealOverTime:
                    HOTHealToTick = 0;
                    break;
                case CurseType.BootsBetterDefense:
                case CurseType.BootsBetterImmunity:
                    if (Game1.player.boots.Value != null)
                    {
                        Game1.player.boots.Value.onUnequip(Game1.player);
                    }
                    break;
                default:
                    break;
            }

            Game1.player.get_FarmerCurses().Remove((int)curseType);
            Game1.player.buffs.Remove(GetBuffId(curseType));
            bool result = !HasCurse(curseType);

            switch (curseType)
            {
                case CurseType.BootsBetterDefense:
                case CurseType.BootsBetterImmunity:
                    if (Game1.player.boots.Value != null)
                    {
                        Game1.player.boots.Value.reloadData();
                        Game1.player.boots.Value.onEquip(Game1.player);
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        public static void RemoveAllCurses()
        {
            foreach (CurseType curseType in Enum.GetValues(typeof(CurseType)))
                RemoveCurse(curseType);
        }
    }
}
