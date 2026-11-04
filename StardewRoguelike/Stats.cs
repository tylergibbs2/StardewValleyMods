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

        /// <summary>
        /// Gets all the statistics as a list of user-friendly strings.
        /// </summary>
        /// <returns>Key:Value formatted strings with parsed statistics data.</returns>
        public List<string> GetLines()
        {
            List<string> lines = new();

            if (EndTime is not null && StartTime is not null)
            {
                TimeSpan span = (TimeSpan)(EndTime - StartTime);
                lines.Add($"{I18n.Stats_Duration()}:{span:hh\\h\\ mm\\m\\ ss\\s}");
            }
            else if (EndTime is null && StartTime is not null)
            {
                TimeSpan span = (TimeSpan)(DateTime.UtcNow - StartTime);
                lines.Add($"{I18n.Stats_Duration()}:{span:hh\\h\\ mm\\m\\ ss\\s}");
            }
            else
                lines.Add($"{I18n.Stats_Duration()}:{I18n.Stats_NotStarted()}");

            if (DinoKillEndTime is not null && StartTime is not null)
            {
                TimeSpan span = (TimeSpan)(DinoKillEndTime - StartTime);
                lines.Add($"{I18n.Stats_CompletionTime()}:{span:hh\\h\\ mm\\m\\ ss\\s}");
            }
            else if (DinoKillEndTime is null && StartTime is not null && EndTime is null)
            {
                TimeSpan span = (TimeSpan)(DateTime.UtcNow - StartTime);
                lines.Add($"{I18n.Stats_CompletionTime()}:{span:hh\\h\\ mm\\m\\ ss\\s}");
            }
            else
                lines.Add($"{I18n.Stats_CompletionTime()}:{I18n.Stats_NotStarted()}");

            lines.Add($"{I18n.Stats_FloorsDescended()}:{FloorsDescended}");
            lines.Add($"{I18n.Stats_MonstersKilled()}:{MonstersKilled}");
            lines.Add($"{I18n.Stats_BossesDefeated()}:{BossesDefeated}");
            lines.Add($"{I18n.Stats_DamageDealt()}:{DamageDealt}");
            lines.Add($"{I18n.Stats_DamageTaken()}:{DamageTaken}");
            lines.Add($"{I18n.Stats_TotalHealing()}:{TotalHealing}");
            lines.Add($"{I18n.Stats_GoldEarned()}:{GoldEarned}");
            lines.Add($"{I18n.Stats_GoldSpent()}:{GoldSpent}");

            return lines;
        }
        /// <summary>
        /// Event handler for checking if certain stats have changed.
        /// This handles checking for gold and HP differences.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="UpdateTickedEventArgs"/> instance containing the event data.</param>
        public void WatchChanges(object? sender, UpdateTickedEventArgs e)
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
