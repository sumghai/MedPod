using RimWorld;
using Verse;

namespace MedPod
{
    public static class AlphaGenesCompatibility
    {
        // __0 refers to ___pawn in original Alpha Genes code
        public static bool SkipIfPawnIsOnMedPod(Pawn __0)
        {            
            if (__0.CurrentBed() is Building_BedMedPod)
            {
                // Skip if the pawn is lying on a MedPod
                return false;
            }
            return true; // Otherwise continue to break bones upon being downed
        }
    }
}