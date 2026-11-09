using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace StardewRoguelike
{
    /// <summary>
    /// Stores the statistics for a run and the methods for printing them.
    /// </summary>
    public class Stats
    {
        public bool HardMode { get; set; } = false;

        public bool Multiplayer { get; set; } = false;

        public int PlayerCount { get; set; } = 0;

        public DateTime? StartTime { get; set; } = null;

        public DateTime? EndTime { get; set; } = null;

        public DateTime? DinoKillEndTime { get; set; } = null;

        public int MonstersKilled { get; set; } = 0;

        public int BossesDefeated { get; set; } = 0;

        public int FloorsDescended { get; set; } = 0;

        public int GoldSpent { get; set; } = 0;

        public int GoldEarned { get; set; } = 0;

        public int TotalHealing { get; set; } = 0;

        public int DamageDealt { get; set; } = 0;

        public int DamageTaken { get; set; } = 0;

        public string? Patch { get; set; } = null;

        public string? Seed { get; set; } = null;

        private int oldMoney = -1;

        private int oldHealth = -1;

        /// <summary>
        /// Resets all the statistics.
        /// </summary>
        public void Reset()
        {
            Multiplayer = false;
            PlayerCount = 0;
            StartTime = null;
            EndTime = null;
            DinoKillEndTime = null;
            MonstersKilled = 0;
            BossesDefeated = 0;
            FloorsDescended = 0;
            GoldSpent = 0;
            GoldEarned = 0;
            TotalHealing = 0;
            DamageDealt = 0;
            DamageTaken = 0;
            Patch = null;
            Seed = null;
        }

        /// <summary>Which stat a row is, so the menu knows how to dress it.</summary>
        /// <remarks>
        /// The identity travels with the row and the look is chosen from it by the
        /// menu. Stats knows what the numbers are; what colour a number is drawn in
        /// and which icon sits beside it is not its business.
        /// </remarks>
        public enum StatId
        {
            Duration,
            CompletionTime,
            FloorsDescended,
            MonstersKilled,
            BossesDefeated,
            DamageDealt,
            DamageTaken,
            TotalHealing,
            GoldEarned,
            GoldSpent
        }

        /// <summary>One line of the stats menu.</summary>
        public readonly record struct StatRow(StatId Id, string Label, string Value);

        /// <summary>A headed group of them.</summary>
        public readonly record struct StatSection(string Title, List<StatRow> Rows);

        /// <summary>How long the run has been going, or went on for.</summary>
        private string Elapsed(DateTime? until)
        {
            if (StartTime is null)
                return I18n.Stats_NotStarted();

            TimeSpan span = (until ?? DateTime.UtcNow) - StartTime.Value;

            // Hours only once there are some. A run is usually minutes, and "00h"
            // in front of every reading is three characters of nothing.
            return span.TotalHours >= 1
                ? $"{span:h\\h\\ mm\\m\\ ss\\s}"
                : $"{span:m\\m\\ ss\\s}";
        }

        /// <summary>
        /// The statistics as the menu shows them: grouped, ordered and formatted.
        /// </summary>
        /// <remarks>
        /// This used to hand back "Label:Value" strings that the menu split on the
        /// colon, which meant no value could ever contain one - a real constraint
        /// on a menu that shows durations - and left the menu with nothing to tell
        /// gold from damage by, so every line was drawn identically in black.
        /// </remarks>
        public List<StatSection> GetSections()
        {
            return new()
            {
                new(I18n.UI_StatsMenu_Section_Progress(), new()
                {
                    new(StatId.Duration, I18n.Stats_Duration(), Elapsed(EndTime)),
                    new(StatId.CompletionTime, I18n.Stats_CompletionTime(), Completion()),
                    new(StatId.FloorsDescended, I18n.Stats_FloorsDescended(), Count(FloorsDescended))
                }),

                new(I18n.UI_StatsMenu_Section_Combat(), new()
                {
                    new(StatId.MonstersKilled, I18n.Stats_MonstersKilled(), Count(MonstersKilled)),
                    new(StatId.BossesDefeated, I18n.Stats_BossesDefeated(), Count(BossesDefeated)),
                    new(StatId.DamageDealt, I18n.Stats_DamageDealt(), Count(DamageDealt)),
                    new(StatId.DamageTaken, I18n.Stats_DamageTaken(), Count(DamageTaken)),
                    new(StatId.TotalHealing, I18n.Stats_TotalHealing(), Count(TotalHealing))
                }),

                new(I18n.UI_StatsMenu_Section_Gold(), new()
                {
                    new(StatId.GoldEarned, I18n.Stats_GoldEarned(), Count(GoldEarned)),
                    new(StatId.GoldSpent, I18n.Stats_GoldSpent(), Count(GoldSpent))
                })
            };
        }

        /// <summary>Shown where a run has no reading to give. An em dash.</summary>
        /// <remarks>
        /// A run that ended without the final kill has no completion time, and
        /// saying "Not Started" of it was wrong twice over - it had been started,
        /// and it was over. A dash is the ordinary way a table says "no value", and
        /// it does not read as a number the way a zero would.
        /// </remarks>
        public const string NoReading = "\u2014";

        /// <summary>
        /// How long the run took to finish, if it did.
        /// </summary>
        /// <remarks>
        /// Counts up while the run is still going, because until it ends this is a
        /// speedrun clock rather than a record.
        /// </remarks>
        private string Completion()
        {
            if (DinoKillEndTime is not null)
                return Elapsed(DinoKillEndTime);

            return StartTime is not null && EndTime is null ? Elapsed(null) : NoReading;
        }

        /// <summary>Grouped by thousands, because damage dealt runs to five figures.</summary>
        private static string Count(int value) => value.ToString("N0");

        /// <summary>
        /// Event handler for checking if certain stats have changed.
        /// This handles checking for gold and HP differences.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="UpdateTickedEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Static, and resolving the stats per call. Subscribing an instance method
        /// would bind whichever screen's stats happened to exist when the handler
        /// was hooked, and then feed it every screen's ticks.
        /// </remarks>
        public static void WatchChanges(object? sender, UpdateTickedEventArgs e)
        {
            ModEntry.ActiveStats.WatchTick();
        }

        private void WatchTick()
        {
            if (Roguelike.CurrentLevel == 0)
                return;

            if (oldMoney == -1)
                oldMoney = Game1.player.Money;

            if (Game1.player.Money != oldMoney && EndTime is null)
            {
                int difference = Game1.player.Money - oldMoney;
                if (difference > 0)
                    GoldEarned += difference;
                else if (difference < 0)
                    GoldSpent += Math.Abs(difference);
                oldMoney = Game1.player.Money;
            }

            if (Game1.player.health != oldHealth && EndTime is null)
            {
                int difference = Game1.player.health - oldHealth;
                if (difference > 0)
                    TotalHealing += difference;
                else if (difference < 0)
                    DamageTaken += Math.Abs(difference);
                oldHealth = Game1.player.health;
            }
        }
    }
}
