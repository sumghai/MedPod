using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace MedPod
{
    public class MedPodMod : Mod
    {
        private static Type workGiver_washPatientType;

        public MedPodMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.MedPod.patches");
            harmony.PatchAll();

            if (ModCompatibility.AndroidTiersIsActive)
            {
                Log.Message("MedPod :: Android Tiers detected!");
            }

            if (ModCompatibility.DbhIsActive)
            {
                Log.Message("MedPod :: Dubs Bad Hygiene detected!");

                // Conditionally patch WorkGiver_washPatient to ignore MedPods
                workGiver_washPatientType = AccessTools.TypeByName("WorkGiver_washPatient");
                MethodInfo original = AccessTools.Method(workGiver_washPatientType, "ShouldBeWashedBySomeone");
                HarmonyMethod postfix = new HarmonyMethod(typeof(Patches.Harmony_DBH_Patches), nameof(Patches.Harmony_DBH_Patches.ShouldBeWashedBySomeonePostfix));
                harmony.Patch(original, postfix: postfix);
            }

            if (ModCompatibility.VteIsActive)
            {
                Log.Message("MedPod :: Vanilla Traits Expanded detected!");
            }
        }

        
    }
}