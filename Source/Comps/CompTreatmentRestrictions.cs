﻿using RimWorld;
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

        public List<HediffDef> NonCriticalTreatableHediffs
        {
            get
            {
                return Props.nonCriticalTreatableHediffs;
            }
        }

        public List<TraitDef> AlwaysTreatableTraits
        {
            get
            {
                return Props.alwaysTreatableTraits;
            }
        }

        public List<HediffDef> UsageBlockingHediffs
        {
            get
            {
                return Props.usageBlockingHediffs;
            }
        }

        public List<TraitDef> UsageBlockingTraits
        {
            get
            {
                return Props.usageBlockingTraits;
            }
        }

        public List<string> DisallowedRaces
        {
            get
            {
                return Props.disallowedRaces;
            }
        }

        public List<XenotypeDef> DisallowedXenotypes
        {
            get
            {
                return Props.disallowedXenotypes;
            }
        }
    }
}