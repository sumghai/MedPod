﻿using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MedPod
{
    public static class MedPodHealthAIUtility
    {
        public static bool ShouldPawnSeekMedPod(Pawn patientPawn)
        {
            return patientPawn.Downed
                    // hasHediffsNeedingTend
                    || patientPawn.health.HasHediffsNeedingTend(false)
                    // hasTendedAndHealingInjury
                    || patientPawn.health.hediffSet.HasTendedAndHealingInjury()
                    // hasImmunizableNotImmuneHediff
                    || patientPawn.health.hediffSet.HasImmunizableNotImmuneHediff()
                    // hasMissingBodyParts
                    || !patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().NullOrEmpty()
                    // hasPermanentInjuries
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.IsPermanent())
                    // hasChronicDiseases
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.chronic)
                    // hasAddictions
                    || patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.IsAddiction);
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
    }
}