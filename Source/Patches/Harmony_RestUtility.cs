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
            return (bedThing is Building_BedMedPod bedMedPod) ? CanUseMedPod(bedMedPod, sleeper): __result;
        }

        public static bool CanUseMedPod(Building_BedMedPod bedMedPod, Pawn pawn)
        {          
            return 
                // MedPod has power
                bedMedPod.powerComp.PowerOn
                // MedPod is not forbidden for the pawn
                && !bedMedPod.IsForbidden(pawn) 
                // Pawn actually has a medical need for a MedPod
                && MedPodHealthAIUtility.ShouldSeekMedPodRest(pawn, bedMedPod.AlwaysTreatableHediffs, bedMedPod.NeverTreatableHediffs, bedMedPod.NonCriticalTreatableHediffs, bedMedPod.UsageBlockingHediffs, bedMedPod.UsageBlockingTraits)
                // Pawn has medical care category that allows MedPod use
                && MedPodHealthAIUtility.HasAllowedMedicalCareCategory(pawn)
                // Pawn type (colonist, slave, prisoner) matches bedtype
                && (pawn.IsColonist == bedMedPod.ForColonists || pawn.IsSlave == bedMedPod.ForSlaves || pawn.IsPrisoner == bedMedPod.ForPrisoners)
                // MedPod hasn't been aborted
                && !bedMedPod.Aborted;
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