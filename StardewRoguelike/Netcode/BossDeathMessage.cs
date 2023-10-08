namespace StardewRoguelike.Netcode
{
    internal class BossDeathMessage
    {
        public string BossName { get; set; } = "";

        public int KillSeconds { get; set; } = 0;

        public BossDeathMessage() { }

        public BossDeathMessage(string bossName, int killSeconds)
        {
            BossName = bossName;
            KillSeconds = killSeconds;
        }
    }
}
