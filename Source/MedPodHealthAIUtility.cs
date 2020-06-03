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
            bool hasPermanentInjuries = (patientPawn.health.hediffSet.GetHediffs<Hediff>().Where(x => x.IsPermanent()).Count() > 0) ? true : false;

            return isDowned || hasHediffsNeedingTend || hasTendedAndHealingInjury || hasImmunizableNotImmuneHediff || hasMissingBodyParts || hasPermanentInjuries;
        }
    }
}