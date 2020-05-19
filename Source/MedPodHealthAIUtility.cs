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
            int missingBodyPartCount = patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().Count();
            bool hasMissingBodyParts = !patientPawn.health.hediffSet.GetMissingPartsCommonAncestors().NullOrEmpty();

            return isDowned || hasHediffsNeedingTend || hasTendedAndHealingInjury || hasImmunizableNotImmuneHediff || hasMissingBodyParts;
        }
    }
}
