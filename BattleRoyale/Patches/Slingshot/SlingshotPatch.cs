using Netcode;
using StardewValley.Projectiles;
using System;

namespace BattleRoyale.Patches
{
    class SlingshotPatch1 : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(BasicProjectile), null, Array.Empty<Type>());

        public static void Postfix(BasicProjectile __instance)
        {
            var r = ModEntry.BRGame.Helper.Reflection;
            r.GetField<NetBool>(__instance, "damagesMonsters").SetValue(new NetBool(false));
        }
    }
}