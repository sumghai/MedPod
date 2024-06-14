﻿using RimWorld;
using System.Collections.Generic;
using Verse.AI;

namespace MedPod
{
    public class JobDriver_PatientGoToMedPod : JobDriver
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
                // Fail if MedPod has lost power, or has Allow Guest disabled while the pawn is en route
                return !BedMedPod.powerComp.PowerOn || ((!pawn.IsColonist && !pawn.IsPrisoner || pawn.GuestStatus == GuestStatus.Guest) && !BedMedPod.allowGuests);
            });
            AddFinishAction(delegate 
            {
                if(BedMedPod.status != Building_BedMedPod.MedPodStatus.Idle && BedMedPod.status != Building_BedMedPod.MedPodStatus.Error)
                {
                    BedMedPod.DischargePatient(pawn, !BedMedPod.Aborted);
                }
            });
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = 1;
            });
            yield return Toils_Bed.GotoBed(MedPodInd);
            yield return Toils_LayDown.LayDown(MedPodInd, true, false);
        }
    }
}