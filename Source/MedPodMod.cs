using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace MedPod
{
    public class MedPodMod : Mod
    {
        private static Type workGiver_washPatientType;
        private static Type targetPatchClass;

        public MedPodMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.MedPod.patches");
            harmony.PatchAll();

            if (ModCompatibility.AlphaGenesIsActive)
            {
                Log.Message("MedPod :: Alpha Genes detected!");

                // Conditionally patch BreakSomeBones patch to patients on MedPods
                targetPatchClass = AccessTools.TypeByName("AlphaGenes_Pawn_HealthTracker_MakeDowned_Patch");
                MethodInfo original = AccessTools.Method(targetPatchClass, "BreakSomeBones");
                HarmonyMethod prefix = new HarmonyMethod(typeof(AlphaGenesCompatibility), nameof(AlphaGenesCompatibility.SkipIfPawnIsOnMedPod));
                harmony.Patch(original, prefix);
            }

            if (ModCompatibility.AndroidTiersIsActive)
            {
                Log.Message("MedPod :: Android Tiers detected!");
            }

            if (ModCompatibility.CombatExtendedIsActive) 
            {
                Log.Message("MedPod :: Combat Extended detected!");
            }

            if (ModCompatibility.DbhIsActive)
            {
                Log.Message("MedPod :: Dubs Bad Hygiene detected!");

                // Conditionally patch WorkGiver_washPatient to ignore MedPods
                workGiver_washPatientType = AccessTools.TypeByName("WorkGiver_washPatient");
                MethodInfo original = AccessTools.Method(workGiver_washPatientType, "ShouldBeWashedBySomeone");
                HarmonyMethod postfix = new HarmonyMethod(typeof(DbhCompatibility), nameof(DbhCompatibility.ShouldBeWashedBySomeonePostfix));
                harmony.Patch(original, postfix: postfix);
            }

            if (ModCompatibility.MechanitePersonaTraitsIsActive)
            {
                Log.Message("MedPod :: Mechanite Persona Traits detected!");
            }
        } 
    }
}