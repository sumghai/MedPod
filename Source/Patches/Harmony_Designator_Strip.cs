using HarmonyLib;
using RimWorld;
using Verse;

namespace MedPod
{
    [HarmonyPatch(typeof(Designator_Strip), nameof(Designator_Strip.CanDesignateThing))]
    public static class Harmony_Designator_Strip_CanDesignateThing_DisallowStrippingMedPodPatients
    {
        static void Postfix(ref AcceptanceReport __result, Thing t)
        {
            if (t is Pawn pawn && !pawn.Dead && pawn.CurrentBed() is Building_BedMedPod)
            {
                __result = false;
            }
        }
    }
}
