using HarmonyLib;
using StardewRoguelike.Mode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StardewRoguelike
{
    abstract class Patch
    {
        protected abstract PatchDescriptor GetPatchDescriptor();

        //Remember Prefix/Postfix should be public and static! Do not use lambdas

        protected class PatchDescriptor
        {
            public Type targetType = null!;
            public string? targetMethodName;
            public Type[]? targetMethodArguments;

            /// <param name="targetType">Use typeof()</param>
            /// <param name="targetMethodName">Null if constructor is desired</param>
            /// <param name="targetMethodArguments">Null if no method abiguity</param>
            public PatchDescriptor(Type targetType, string targetMethodName, Type[]? targetMethodArguments = null)
            {
                this.targetType = targetType;
                this.targetMethodName = targetMethodName;
                this.targetMethodArguments = targetMethodArguments;
            }

            public PatchDescriptor(List<string> possibleTypes, string targetMethodName, Type[]? targetMethodArguments = null)
            {
                foreach (string type in possibleTypes)
                {
                    if (AccessTools.TypeByName(type) is Type foundType)
                    {
                        targetType = foundType;
                        break;
                    }
                }

                this.targetMethodName = targetMethodName;
                this.targetMethodArguments = targetMethodArguments;
            }
        }


        private void ApplyPatch(Harmony harmonyInstance)
        {
            var patchDescriptor = GetPatchDescriptor();

            MethodBase targetMethod;

            if (string.IsNullOrEmpty(patchDescriptor.targetMethodName))
                targetMethod = patchDescriptor.targetType.GetConstructor(patchDescriptor.targetMethodArguments ?? Array.Empty<Type>())!;
            else if (patchDescriptor.targetMethodArguments is not null)
                targetMethod = patchDescriptor.targetType.GetMethod(patchDescriptor.targetMethodName, patchDescriptor.targetMethodArguments)!;
            else
                targetMethod = patchDescriptor.targetType.GetMethod(patchDescriptor.targetMethodName, (BindingFlags)62)!;

            try
            {
                MethodInfo prefix = AccessTools.Method(this.GetType(), "Prefix");
                MethodInfo postfix = AccessTools.Method(this.GetType(), "Postfix");
                MethodInfo transpiler = AccessTools.Method(this.GetType(), "Transpiler") ?? AccessTools.Method(this.GetType(), "Transpile");

                HarmonyMethod hmPrefix = null!;
                HarmonyMethod hmPostfix = null!;
                HarmonyMethod hmTranspile = null!;

                if (prefix is not null) hmPrefix = new HarmonyMethod(prefix);
                if (postfix is not null) hmPostfix = new HarmonyMethod(postfix);
                if (transpiler is not null) hmTranspile = new HarmonyMethod(transpiler);

                harmonyInstance.Patch(targetMethod,
                    prefix: hmPrefix,
                    postfix: hmPostfix,
                    transpiler: hmTranspile);
            }
            catch (Exception e)
            {
                Console.WriteLine(GetType().FullName);
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>Patches that implement routing and stay applied in vanilla sessions.</summary>
        public const string AlwaysActiveId = "tylergibbs2.StardewRoguelike.core";

        /// <summary>Gameplay patches, applied only while a roguelike run owns the game.</summary>
        public const string ModeScopedId = "tylergibbs2.StardewRoguelike.mode";

        /// <summary>
        /// Applies the patches that have to be present before the player has chosen
        /// a mode, so the title screen can offer the roguelike at all.
        /// </summary>
        public static void PatchAlwaysActive()
        {
            Harmony harmony = new(AlwaysActiveId);

            foreach (Type type in AttributePatches())
            {
                if (type.GetCustomAttribute<AlwaysActiveAttribute>() is not null)
                    harmony.CreateClassProcessor(type).Patch();
            }
        }

        /// <summary>
        /// Applies every gameplay patch. Mode scoped is the default: a patch only
        /// escapes this by being marked <see cref="AlwaysActiveAttribute"/>, so new
        /// patches are undone on exit without anyone having to opt in.
        /// </summary>
        public static void PatchModeScoped()
        {
            Harmony harmony = new(ModeScopedId);

            foreach (Type type in (from type in Assembly.GetExecutingAssembly().GetTypes()
                                   where type.IsClass && type.BaseType == typeof(Patch)
                                   select type))
                ((Patch)Activator.CreateInstance(type)!).ApplyPatch(harmony);

            foreach (Type type in AttributePatches())
            {
                if (type.GetCustomAttribute<AlwaysActiveAttribute>() is null)
                    harmony.CreateClassProcessor(type).Patch();
            }
        }

        /// <summary>Removes every gameplay patch, handing the game back to vanilla.</summary>
        public static void UnpatchModeScoped()
        {
            new Harmony(ModeScopedId).UnpatchAll(ModeScopedId);
        }

        /// <summary>
        /// The [HarmonyPatch] annotated classes. Harmony's own PatchAll takes the
        /// whole assembly at once, which is exactly what we cannot do here.
        /// </summary>
        private static IEnumerable<Type> AttributePatches()
        {
            return from type in Assembly.GetExecutingAssembly().GetTypes()
                   where type.IsClass && type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length > 0
                   select type;
        }
    }
}
