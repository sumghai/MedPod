using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MedPod
{
    public static class MedPodHealthAIUtility
    {
        public static bool ShouldSeekMedPodRest(Pawn patientPawn, List<HediffDef> alwaysTreatableHediffs, List<HediffDef> neverTreatableHediffs, List<HediffDef> nonCriticalTreatableHediffs)
        {           
            return 
                    // Is downed
                    patientPawn.Downed
                    // Has hediffs requiring tending
                    || patientPawn.health.HasHediffsNeedingTend(false)
                    // Has tended and healing injuries
                    || patientPawn.health.hediffSet.HasTendedAndHealingInjury()
                    // Has immunizable but not yet immune hediffs
                    || patientPawn.health.hediffSet.HasImmunizableNotImmuneHediff()
                    // Has missing body parts
                    || (!patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().NullOrEmpty() && !neverTreatableHediffs.Contains(HediffDefOf.MissingBodyPart))
                    // Has permanent injuries (excluding those blacklisted from MedPod treatment)
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.IsPermanent() && !neverTreatableHediffs.Contains(x.def))
                    // Has chronic diseases (excluding those blacklisted or greylisted from MedPod treatment)
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.chronic && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def))
                    // Has addictions (excluding those blacklisted or greylisted from MedPod treatment)
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.IsAddiction && !neverTreatableHediffs.Contains(x.def) && !nonCriticalTreatableHediffs.Contains(x.def))
                    // Has hediffs that are always treatable by MedPods
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => alwaysTreatableHediffs.Contains(x.def));
        }

        public static bool IsValidRaceForMedPod(Pawn patientPawn, List<string> disallowedRaces)
        {
            string patientRace = patientPawn.def.ToString();
            if (!disallowedRaces.NullOrEmpty())
            {
                return !disallowedRaces.Contains(patientRace);
            }
            return true;
        }

        public static bool HasUsageBlockingHediffs(Pawn patientPawn, List<HediffDef> usageBlockingHediffs)
        {
            return patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => usageBlockingHediffs.Contains(x.def));
        }

        public static bool HasUsageBlockingTraits(Pawn patientPawn, List<TraitDef> usageBlockingTraits)
        {
            return patientPawn.story?.traits.allTraits.Any(x => usageBlockingTraits.Contains(x.def)) ?? false;
        }
    }
}