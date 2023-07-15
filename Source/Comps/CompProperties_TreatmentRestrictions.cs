using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MedPod
{
    class CompProperties_TreatmentRestrictions : CompProperties
    {
        public List<HediffDef> alwaysTreatableHediffs = null;

        public List<HediffDef> neverTreatableHediffs = null;

        public List<HediffDef> nonCriticalTreatableHediffs = null;

        public List<TraitDef> alwaysTreatableTraits = null;

        public List<HediffDef> usageBlockingHediffs = null;

        public List<TraitDef> usageBlockingTraits = null;

        public List<string> disallowedRaces = null;

        [MayRequireBiotech]
        public List<XenotypeDef> disallowedXenotypes = null;

        public CompProperties_TreatmentRestrictions()
        {
            compClass = typeof(CompTreatmentRestrictions);
        }
    }
}