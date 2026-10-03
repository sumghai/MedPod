using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MedPod
{
    [HarmonyPatch(typeof(WorkGiver_Warden_TakeToBed), "JobOnThing")]
    public static class Harmony_PatchWardenTakeToBed_FromMedPod
    {
        public static bool Prefix(Pawn pawn, Thing t, ref Job __result)
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