using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MedPod
{
    public static class MedPodHealthAIUtility
    {
        public static bool ShouldPawnSeekMedPod(Pawn patientPawn)
        {
            bool isDowned = patientPawn.Downed;
            bool hasHediffsNeedingTend = patientPawn.health.HasHediffsNeedingTend(false);
            bool hasTendedAndHealingInjury = patientPawn.health.hediffSet.HasTendedAndHealingInjury();
            bool hasImmunizableNotImmuneHediff = patientPawn.health.hediffSet.HasImmunizableNotImmuneHediff();
            bool hasMissingBodyParts = !patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().NullOrEmpty();
            bool hasPermanentInjuries = patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.IsPermanent());
            bool hasChronicDiseases = patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.chronic);
            bool hasAddictions = patientPawn.health.hediffSet.GetHediffs<Hediff>().Any(x => x.def.IsAddiction); ;

            return isDowned || hasHediffsNeedingTend || hasTendedAndHealingInjury || hasImmunizableNotImmuneHediff || hasMissingBodyParts || hasPermanentInjuries || hasChronicDiseases || hasAddictions;
        }

        public static bool IsValidRaceForMedPod(Pawn patientPawn, List<string> disallowedRaces)
        {
            if (!disallowedRaces.NullOrEmpty())
            {
                foreach (string currentRace in disallowedRaces)
                {
                    if (patientPawn.def.ToString() == currentRace)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}