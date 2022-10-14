using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod
{
    // Check if patient is (still) allowed to use the MedPod
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
    public static class RestUtility_CanUseBedNow_AddMedPodFailConditions
    {
        public static bool Postfix(bool __result, Thing bedThing, Pawn sleeper)
        {
            return (bedThing is Building_BedMedPod bedMedPod) ? bedMedPod.powerComp.PowerOn && !bedMedPod.IsForbidden(sleeper) && MedPodHealthAIUtility.ShouldSeekMedPodRest(sleeper, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs, bedMedPod.NonCriticalTreatableHediffs) && !bedMedPod.Aborted : __result;
        }
    }
    
    // Exclude our MedPod beds from normal checks for valid beds, so that regular vanilla bed rest WorkGivers never use them
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
    public static class RestUtility_IsValidBedFor_IgnoreMedPods
    {
        public static bool Prefix(ref bool __result, Thing bedThing)
        {
            if (bedThing is Building_BedMedPod)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

}