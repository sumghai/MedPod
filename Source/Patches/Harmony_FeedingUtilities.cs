using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace MedPod.Patches
{
    // Prevent Doctors/Wardens from feeding patients if:
    // - The patient is lying on a MedPod
    // - The MedPod is powered
    // (as they would get smacked in the face by the MedPod's moving gantry)
    [HarmonyPatch]
    static class ShouldBeFed_IgnoreMedPods
    {
        static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return AccessTools.Method(typeof(FeedPatientUtility), "ShouldBeFed");
            yield return AccessTools.Method(typeof(WardenFeedUtility), "ShouldBeFed");
        }

        static void Postfix(ref bool __result, Pawn p)
        {
            if (p.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
            {
                __result = false;
            }
        }
    }
}
