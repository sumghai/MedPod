using System.Collections.Generic;
using Verse;

namespace MedPod
{
    class CompProperties_TreatmentRestrictions : CompProperties
    {
        public List<HediffDef> alwaysTreatableHediffs = null;

        public List<HediffDef> neverTreatableHediffs = null;

        public List<HediffDef> nonCriticalTreatableHediffs = null;

        public List<HediffDef> usageBlockingHediffs = null;

        public List<string> disallowedRaces = null;

        public CompProperties_TreatmentRestrictions()
        {
            compClass = typeof(CompTreatmentRestrictions);
        }
    }
}