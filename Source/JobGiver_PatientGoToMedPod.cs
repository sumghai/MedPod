using Verse;
using Verse.AI;

namespace MedPod
{
    public class JobGiver_PatientGoToMedPod : ThinkNode
    {
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            Building_BedMedPod bedMedPod = MedPodRestUtility.FindBestMedPod(pawn, pawn);
            if (bedMedPod != null)
            {
                return new ThinkResult(JobMaker.MakeJob(MedPodDef.PatientGoToMedPod, bedMedPod), this);
            }
                        
            return ThinkResult.NoJob;
        }
    }
}
