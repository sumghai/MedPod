using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class WorkGiver_DoctorRescueToMedPod : WorkGiver_RescueDowned
    {
		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn patient = t as Pawn;

			if (patient == null || !patient.Downed || patient.Faction != pawn.Faction || patient.CurrentBed()?.def.thingClass == typeof(Building_BedMedPod) || !pawn.CanReserve(patient, 1, -1, null, forced) || GenAI.EnemyIsNear(patient, MinDistFromEnemy))
			{
				return false;
			}
			Building_BedMedPod bedMedPod = FindBestMedPod(pawn, patient);
			if (bedMedPod != null && patient.CanReserve(bedMedPod))
			{
				return true;
			}
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Pawn patient = t as Pawn;
			Building_BedMedPod bedMedPod = FindBestMedPod(pawn, patient);
			Job job = JobMaker.MakeJob(MedPodDef.RescueToMedPod, patient, bedMedPod);
			job.count = 1;
			return job;
		}

		public static Building_BedMedPod FindBestMedPod(Pawn pawn, Pawn patient)
		{
			List<ThingDef> medPodDefsBestToWorst = RestUtility.bedDefsBestToWorst_Medical.Where(x => x.thingClass == typeof(Building_BedMedPod)).ToList();

			for (int i = 0; i < medPodDefsBestToWorst.Count; i++)
			{				
				ThingDef thingDef = medPodDefsBestToWorst[i];

				if (!RestUtility.CanUseBedEver(patient, thingDef))
				{
					continue;
				}

				for (int j = 0; j < 2; j++)
				{
					Danger maxDanger2 = (j == 0) ? Danger.None : Danger.Deadly;

                    bool validator(Thing t)
                    {
                        Building_BedMedPod bedMedPod = t as Building_BedMedPod;

                        bool isMedicalBed = ((Building_BedMedPod)bedMedPod).Medical;

                        bool patientDangerCheck = (int)bedMedPod.Position.GetDangerFor(patient, patient.Map) <= (int)maxDanger2;

						bool isValidBedFor = MedPodHealthAIUtility.IsValidMedPodFor(bedMedPod, patient, pawn, patient.GuestStatus);

                        bool result = isMedicalBed && patientDangerCheck && isValidBedFor;

                        return result;
                    }

					Building_BedMedPod bedMedPod = (Building_BedMedPod)GenClosest.ClosestThingReachable(patient.Position, patient.Map, ThingRequest.ForDef(thingDef), PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, validator);

					if (bedMedPod != null)
					{
						return bedMedPod;
					}
				}
			}

			return null;
		}
	}
}
