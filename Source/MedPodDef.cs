using RimWorld;
using Verse;

namespace MedPod
{
    [DefOf]
    public static class MedPodDef
    {
        public class MayRequireMechanitePersonaTraitsModAttribute : MayRequireAttribute
        {
            public MayRequireMechanitePersonaTraitsModAttribute()
                : base("ImJustJoshin.MechanitePersonaTraits")
            { }
        }
        
        static MedPodDef()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MedPodDef));
        }

        public static HediffDef MedPod_InducedComa;

        public static JobDef CarryToMedPod;
        public static JobDef PatientGoToMedPod;
        public static JobDef RescueToMedPod;

        public static ThingDef MedPodInvisibleBlocker;

        [MayRequireMechanitePersonaTraitsMod]
        public static NeedDef MPT_Need_MechaniteFactory;
    }
}