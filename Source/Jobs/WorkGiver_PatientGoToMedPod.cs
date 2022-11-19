using RimWorld;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_PatientGoToMedPod : WorkGiver
    {
        public static JobGiver_PatientGoToMedPod jgpgtmp = new JobGiver_PatientGoToMedPod();

        public override Job NonScanJob(Pawn pawn)
        {
            ThinkResult thinkResult = jgpgtmp.TryIssueJobPackage(pawn, default(JobIssueParams));
            if (thinkResult.IsValid)
            {
                return thinkResult.Job;
            }
            return null;
        }
    }
}