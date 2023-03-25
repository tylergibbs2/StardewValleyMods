using StardewModdingAPI;

namespace Circuit
{
    internal static class Logger
    {
        public static void Log(string message, LogLevel logLevel)
        {
            ModEntry.Instance.Monitor.Log(message, logLevel);
        }
    }
}
