using Verse;
using Verse.AI;

namespace MedPod
{
    public class JobGiver_PatientGoToMedPod : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            Building_BedMedPod bedMedPod = MedPodRestUtility.FindBestMedPod(pawn, pawn);
            return (bedMedPod != null) ? JobMaker.MakeJob(MedPodDef.PatientGoToMedPod, bedMedPod) : null;
        }
    }
}
