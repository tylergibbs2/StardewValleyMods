namespace BattleRoyale
{
    class ModConfig
    {
        public int TimeInSecondsBetweenRounds { get; set; } = 15;
        public int TimeInMillisecondsBetweenPlayerJoiningAndServerExpectingTheirVersionNumber { get; set; } = 60000;
        public int PlayerLimit { get; set; } = 125;
        public int StormDamagePerSecond { get; set; } = 5;
    }
}
