using RimWorld;
using Verse;

namespace MedPod.Patches
{
    static class Harmony_DBH_Patches
    {
        public static void ShouldBeWashedBySomeonePostfix(Pawn pawn, ref bool __result)
        {
            if (pawn.CurrentBed() is Building_BedMedPod bedMedPod && bedMedPod.powerComp.PowerOn)
            {
                __result = false;
            }
        }
    }
}
