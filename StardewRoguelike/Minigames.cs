using StardewModdingAPI.Utilities;
using StardewValley.Minigames;
using StardewValley;

namespace StardewRoguelike
{
    public static class Minigames
    {
        private static readonly PerScreen<int> ScreenPrairieKingKills = new(() => 0);

        private static readonly PerScreen<int> ScreenJunimoKartScore = new(() => 0);

        /// <summary>What this player has shot down in Journey of the Prairie King.</summary>
        /// <remarks>
        /// Per screen, because a cabinet is played by one player. Shared, two local
        /// players' scores piled into one counter and whoever quit first was paid
        /// for both and then zeroed it - and if that player had no Gamer perk, the
        /// payout was skipped but the reset was not, so both scores were binned and
        /// nobody was paid for either.
        /// </remarks>
        public static int PrairieKingKills
        {
            get => ScreenPrairieKingKills.Value;
            set => ScreenPrairieKingKills.Value = value;
        }

        /// <summary>The same for Junimo Kart.</summary>
        public static int JunimoKartScore
        {
            get => ScreenJunimoKartScore.Value;
            set => ScreenJunimoKartScore.Value = value;
        }

        public static void MinigameClosed(IMinigame minigame)
        {
            if (minigame is MineCart cartGame)
            {
                int score = (int)cartGame.GetType().GetField("score", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cartGame)!;
                JunimoKartScore += score;
            }

            if (Perks.HasPerk(Perks.PerkType.Gamer))
            {
                int goldAmount = (JunimoKartScore / 3333) + (PrairieKingKills / 5);
                if (goldAmount > 0)
                {
                    SObject gold = new("384", goldAmount);
                    Game1.player.addItemByMenuIfNecessaryElseHoldUp(gold);
                }
            }

            PrairieKingKills = 0;
            JunimoKartScore = 0;
            Game1.player.jotpkProgress.Value = null;
        }
    }
}
