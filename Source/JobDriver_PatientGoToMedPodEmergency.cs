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
            Log.Warning("MedPod :: JobDriver_PatientGoToMedPodEmergency.MakeNewToils() - Making new toils for " + BedMedPod.ToString() + " ("+ BedMedPod.Label.ToString() + ")");
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
            Log.Warning("\tReturning Toil for going to bed (target=" + BedMedPod.ToString() + ")");
            yield return Toils_Bed.GotoBed(MedPodInd);
            Log.Warning("\tReturning Toil for laying down (target=" + BedMedPod.ToString() + ")");
            yield return Toils_LayDown.LayDown(MedPodInd, true, lookForOtherJobs: false);
        }
    }
}