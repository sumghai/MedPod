using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class JobDriver_PatientGoToMedPodEmergency : JobDriver
    {
        private const TargetIndex MedPodInd = TargetIndex.A;

        protected Building_BedMedPod BedMedPod => (Building_BedMedPod)job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(BedMedPod, job, 1, -1, null, errorOnFailed);
        }

        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(MedPodInd));
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(MedPodInd);
            this.FailOnBurningImmobile(MedPodInd);
            this.FailOn(delegate
            {
                //Fail if MedPod has no power
                return !BedMedPod.powerComp.PowerOn;
            });
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = 1;
            });
            yield return Toils_Bed.GotoBed(MedPodInd);
            yield return Toils_LayDown.LayDown(MedPodInd, true, lookForOtherJobs: false);
        }
    }
}