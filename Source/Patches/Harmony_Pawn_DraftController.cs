using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MedPod
{
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.GetGizmos))]
    public static class Harmony_Pawn_DraftController_DisallowDraftingMedPodPatients
    {
        static void Postfix(Pawn_DraftController __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = PatchGetGizmos(__instance, __result);
        }

        static IEnumerable<Gizmo> PatchGetGizmos(Pawn_DraftController __instance, IEnumerable<Gizmo> __result)
        {
            Pawn patientPawn = __instance.pawn;
            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Toggle toggleCommand)
                {
                    if (toggleCommand.defaultDesc == "CommandToggleDraftDesc".Translate() && patientPawn.CurrentBed() is Building_BedMedPod)
                    {
                        toggleCommand.Disable("MedPod_CommandGizmoDisabled_PatientReceivingTreatment".Translate(patientPawn.LabelShort, patientPawn.CurrentBed()));
                        yield return toggleCommand;
                        continue;
                    }
                }

                yield return gizmo;
            }
        }
    }
}
