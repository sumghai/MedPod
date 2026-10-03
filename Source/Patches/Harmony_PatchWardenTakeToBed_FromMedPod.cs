using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MedPod
{
    // Wardens looking for prisoners to take back to their beds should ignore prisoners already on MedPods
    // This mitigates a rare race condition where a warden who has just carried a prisoner to a MedPod fails to register it as the prisoner's current bed,
    // resulting in prisoners being carried back and forth between the MedPod and prison bed in an infinite loop
    [HarmonyPatch(typeof(WorkGiver_Warden_TakeToBed), nameof(WorkGiver_Warden_TakeToBed.JobOnThing))]
    public static class Harmony_WorkGiver_Warden_TakeToBed_JobOnThing_IgnorePrisonersOnMedPods
    {
        public static bool Prefix(Thing t, ref Job __result)
        {
            if (t is Pawn prisoner && prisoner.IsPrisonerOfColony && prisoner.CurrentBed() is Building_BedMedPod medPod)
            {
                if (MedPodHealthAIUtility.ShouldSeekMedPodRest(prisoner, medPod))
                {
                    __result = null;
                    return false;
                }
            }

            return true;
        }
    }
}