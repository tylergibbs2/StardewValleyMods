using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Bosses;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StardewRoguelike
{
    /// <summary>
    /// Test scaffolding that plays the game: walks the farmer to the nearest monster
    /// and swings the equipped weapon through <see cref="MeleeWeapon.DoDamage"/>, the
    /// same entry point a real swing uses, then reports what happened.
    ///
    /// This exists so a run can be exercised from the SMAPI console without a human
    /// at the keyboard. It drives the real combat paths rather than shortcutting them,
    /// so monster damage, enchantments, curses, drops, boss phases and player death
    /// all behave as they would in play.
    /// </summary>
    internal static class DebugAutoFight
    {
        /// <summary>Ticks between swings, roughly a sword's swing rate.</summary>
        private const int SwingCooldownTicks = 22;

        /// <summary>Ticks of no progress before the farmer is considered stuck on geometry.</summary>
        private const int StuckTicks = 90;

        /// <summary>How close the farmer gets before swinging.</summary>
        private const float MeleeRange = 72f;

        private static bool running;
        private static int ticksLeft;
        private static int cooldown;
        private static int swings;
        private static int kills;
        private static int startHealth;
        private static float lastDistance = float.MaxValue;
        private static int stuckFor;
        private static readonly Dictionary<string, string> LastBossState = new();
        private static readonly Dictionary<string, int> LastBossHealthBucket = new();

        public static void Start(int seconds)
        {
            if (running)
            {
                Log("already running");
                return;
            }

            if (!Context.IsWorldReady)
            {
                Log("no save loaded");
                return;
            }

            EquipWeapon();

            running = true;
            ticksLeft = seconds * 60;
            cooldown = 0;
            swings = 0;
            kills = 0;
            startHealth = Game1.player.health;
            lastDistance = float.MaxValue;
            stuckFor = 0;
            LastBossState.Clear();
            LastBossHealthBucket.Clear();

            ModEntry.Events.GameLoop.UpdateTicked += OnTick;
            Log($"fighting for {seconds}s on {Game1.player.currentLocation?.Name} " +
                $"({CountMonsters()} monsters, player {Game1.player.health}/{Game1.player.maxHealth} hp)");
        }

        public static void Stop(string reason)
        {
            if (!running)
                return;

            running = false;
            ModEntry.Events.GameLoop.UpdateTicked -= OnTick;
            Log($"stopped ({reason}): {swings} swings, {kills} kills, " +
                $"{CountMonsters()} monsters left, player {Game1.player.health}/{Game1.player.maxHealth} hp " +
                $"(started {startHealth})");
        }

        private static int CountMonsters()
        {
            return Game1.player.currentLocation?.characters.OfType<Monster>().Count(m => m.Health > 0) ?? 0;
        }

        private static void EquipWeapon()
        {
            if (Game1.player.CurrentTool is MeleeWeapon)
                return;

            for (int i = 0; i < Game1.player.Items.Count; i++)
            {
                if (Game1.player.Items[i] is MeleeWeapon)
                {
                    Game1.player.CurrentToolIndex = i;
                    Log($"equipped {Game1.player.CurrentTool?.DisplayName}");
                    return;
                }
            }

            Log("no melee weapon in inventory");
        }

        private static void OnTick(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player.currentLocation is null)
                return;

            if (Game1.player.health <= 0)
            {
                Stop("player died");
                return;
            }

            if (--ticksLeft <= 0)
            {
                Stop("time up");
                return;
            }

            GameLocation location = Game1.player.currentLocation;
            ReportBosses(location);

            Monster? target = location.characters
                .OfType<Monster>()
                .Where(m => m.Health > 0)
                .OrderBy(m => Vector2.DistanceSquared(m.Position, Game1.player.Position))
                .FirstOrDefault();

            if (target is null)
            {
                Stop("floor cleared");
                return;
            }

            Vector2 delta = target.Position - Game1.player.Position;
            float distance = delta.Length();
            Game1.player.faceGeneralDirection(target.Position);

            if (distance > MeleeRange)
            {
                WalkToward(delta, distance);
                return;
            }

            lastDistance = distance;
            stuckFor = 0;

            if (cooldown > 0)
            {
                cooldown--;
                return;
            }

            if (Game1.player.CurrentTool is not MeleeWeapon weapon)
            {
                Stop("no weapon equipped");
                return;
            }

            int healthBefore = target.Health;
            string name = target.Name;

            Point aim = target.StandingPixel;
            weapon.DoDamage(location, aim.X, aim.Y, Game1.player.FacingDirection, 1, Game1.player);

            swings++;
            cooldown = SwingCooldownTicks;

            if (target.Health <= 0 && healthBefore > 0)
            {
                kills++;
                Log($"killed {name} (swing {swings})");
            }
        }

        /// <summary>
        /// Walks toward the target using the game's own movement, so walls and
        /// obstacles stop the farmer the way they stop a player.
        ///
        /// Mine layouts can wall the farmer off from a monster entirely, and a bot
        /// that only presses toward it would stall the whole test; if no ground has
        /// been made for a while it slides across instead, and says so.
        /// </summary>
        private static void WalkToward(Vector2 delta, float distance)
        {
            if (distance < lastDistance - 1f)
            {
                lastDistance = distance;
                stuckFor = 0;
            }
            else
            {
                stuckFor++;
            }

            if (stuckFor > StuckTicks)
            {
                Log($"stuck {StuckTicks} ticks at {distance:F0}px, sliding past geometry");
                Game1.player.Position += Vector2.Normalize(delta) * 24f;
                stuckFor = 0;
                lastDistance = float.MaxValue;
                return;
            }

            // 0 = up, 1 = right, 2 = down, 3 = left
            bool horizontalFirst = Math.Abs(delta.X) > Math.Abs(delta.Y);
            int primary = horizontalFirst ? (delta.X > 0 ? 1 : 3) : (delta.Y > 0 ? 2 : 0);
            int secondary = horizontalFirst ? (delta.Y > 0 ? 2 : 0) : (delta.X > 0 ? 1 : 3);

            Game1.player.tryToMoveInDirection(primary, isFarmer: true, damagesFarmer: 0, glider: false);
            if (stuckFor > 10)
                Game1.player.tryToMoveInDirection(secondary, isFarmer: true, damagesFarmer: 0, glider: false);
        }

        /// <summary>Logs boss health milestones and phase changes as they happen.</summary>
        private static void ReportBosses(GameLocation location)
        {
            foreach (Monster monster in location.characters.OfType<Monster>())
            {
                if (monster is not IBossMonster boss)
                    continue;

                string key = monster.Name;

                int bucket = monster.MaxHealth > 0 ? monster.Health * 10 / monster.MaxHealth : 0;
                if (!LastBossHealthBucket.TryGetValue(key, out int lastBucket) || bucket != lastBucket)
                {
                    LastBossHealthBucket[key] = bucket;
                    Log($"{boss.DisplayName}: {monster.Health}/{monster.MaxHealth} hp ({bucket * 10}%)");
                }

                string state = ReadState(monster);
                if (state.Length > 0 && (!LastBossState.TryGetValue(key, out string? lastState) || state != lastState))
                {
                    LastBossState[key] = state;
                    Log($"{boss.DisplayName}: phase -> {state}");
                }
            }
        }

        /// <summary>Reads whatever NetEnum state fields a boss declares, for phase reporting.</summary>
        private static string ReadState(Monster monster)
        {
            List<string> parts = new();

            foreach (FieldInfo field in monster.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.FieldType.IsGenericType || !field.FieldType.Name.StartsWith("NetEnum"))
                    continue;

                object? netField = field.GetValue(monster);
                object? value = netField?.GetType().GetProperty("Value")?.GetValue(netField);
                if (value is not null)
                    parts.Add($"{field.Name}={value}");
            }

            return string.Join(" ", parts);
        }

        private static void Log(string message)
        {
            ModEntry.ModMonitor.Log($"[autofight] {message}", LogLevel.Info);
        }
    }
}
