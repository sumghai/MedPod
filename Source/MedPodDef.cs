using RimWorld;
using Verse;

namespace MedPod
{
    [DefOf]
    public static class MedPodDef
    {
        static MedPodDef()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MedPodDef));
        }

        public static JobDef PatientGoToMedPodEmergency;

        public static ThingDef MedPodInvisibleBlocker;
    }
}