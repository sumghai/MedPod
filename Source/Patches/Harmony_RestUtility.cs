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
                && MedPodHealthAIUtility.ShouldSeekMedPodRest(pawn, bedMedPod)
                // Pawn has medical care category that allows MedPod use
                && MedPodHealthAIUtility.HasAllowedMedicalCareCategory(pawn)
                // Pawn type (colonist, slave, prisoner, guest) matches bedtype
                && MedPodRestUtility.IsValidBedForUserType(bedMedPod, pawn)
                // MedPod hasn't been aborted
                && !bedMedPod.Aborted;
        }
    }
}