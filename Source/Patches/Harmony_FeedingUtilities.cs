using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace MedPod
{
    // Prevent Doctors/Wardens from feeding patients if:
    // - The patient is lying on a MedPod
    // - The MedPod is powered
    // (as they would get smacked in the face by the MedPod's moving gantry)
    [HarmonyPatch]
    public static class ShouldBeFed_IgnoreMedPods
    {
        public static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(FeedPatientUtility), "ShouldBeFed");
            yield return AccessTools.Method(typeof(WardenFeedUtility), "ShouldBeFed");
        }

        // Run this before Dubs Bad Hygiene, so that the mod's associated administer fluid jobs are skipped
        // when the patient is on a MedPod
        [HarmonyBefore(new string[] { "Dubwise.DubsBadHygiene" })]
        public static void Postfix(ref bool __result, Pawn p)
        {
            if (p.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
            {
                __result = false;
            }
        }
    }
}
