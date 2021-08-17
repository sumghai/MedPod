using HarmonyLib;
using Verse;
using Verse.AI;

namespace MedPod.Patches
{
    // Remove induced coma hediff and wake patient if they are accidentally kicked off a MedPod
    // This handles an edge case where the player prioritizes Patient B to use a MedPod while it is already treating Patient A
    [HarmonyPatch(typeof(JobDriver_WaitDowned), nameof(JobDriver_WaitDowned.DecorateWaitToil))]
    static class JobDriver_WaitDowned_DecorateWaitToil_WakePatientIfKickedOffMedPod
    {
        static void Prefix(Pawn ___pawn)
        {
            if (___pawn.health.hediffSet.hediffs.Any((Hediff x) => x.def.defName == "MedPod_InducedComa"))
            {
                Building_BedMedPod.WakePatient(___pawn, false);
            }
        }
    }
}
