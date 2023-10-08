namespace StardewRoguelike.Netcode
{
    internal class RespawnMessage
    {
        public int RespawnLevel;

        public RespawnMessage() { }

        public RespawnMessage(int level)
        {
            RespawnLevel = level;
        }
    }
}
