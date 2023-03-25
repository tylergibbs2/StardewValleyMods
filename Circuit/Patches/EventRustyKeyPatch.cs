using Circuit.Events;
using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Event), nameof(Event.command_rustyKey))]
    internal class EventRustyKeyPatch
    {
        public static void Postfix()
        {
            return;
            //if (!ModEntry.ShouldPatch(EventType.RepairedServices))
              //  return;

            RepairedServices evt = (RepairedServices)EventManager.GetCurrentEvent()!;
            evt.AlreadyHasRustyKey = true;
        }
    }
}
