using System.Collections.Generic;
using Verse;

namespace MedPod
{
    public class CompTreatmentRestrictions : ThingComp
    {
        private CompProperties_TreatmentRestrictions Props
        {
            get
            {
                return (CompProperties_TreatmentRestrictions)props;
            }
        }

        public List<HediffDef> AlwaysTreatableHediffs
        {
            get
            {
                return Props.alwaysTreatableHediffs;
            }
        }

        public List<HediffDef> NeverTreatableHediffs
        {
            get
            {
                return Props.neverTreatableHediffs;
            }
        }

        public List<HediffDef> UsageBlockingHediffs
        {
            get
            {
                return Props.usageBlockingHediffs;
            }
        }

        public List<string> DisallowedRaces
        {
            get
            {
                return Props.disallowedRaces;
            }
        }
    }
}