using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod
{
    // Check if patient is (still) allowed to use the MedPod
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
    public static class Harmony_RestUtility_CanUseBedNow_AddMedPodFailConditions
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

    // Exclude MedPod beds as possible prisoner beds when capturing new prisoners
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.IsValidBedFor))]
    public static class Harmony_RestUtility_IsValidBedFor_ExcludeMedPodsWhenCapturingNewPrisoners
    {
        public static void Postfix(ref bool __result, Thing bedThing, Pawn sleeper, GuestStatus? guestStatus = null) 
        {
            // !sleeper.IsPrisonerOfColony and GuestStatus.Prisoner indicates that the target sleeper pawn
            // is currently not a prisoner of the player colony (but is about to be!)
            if (__result && !sleeper.IsPrisonerOfColony && guestStatus == GuestStatus.Prisoner && bedThing is Building_BedMedPod)
            {
                __result = false;
            }
        }
    }
}