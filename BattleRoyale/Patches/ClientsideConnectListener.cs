using StardewValley.Network;

namespace BattleRoyale.Patches
{
    class ClientsideConnectListener : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Client), "receiveServerIntroduction");

        public static void Postfix(Client __instance)
        {
            AutoKicker.SendMyVersionToTheServer(__instance);
        }
    }
}
