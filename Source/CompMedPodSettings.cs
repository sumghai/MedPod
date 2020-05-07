using Verse;

namespace MedPod
{
    public class CompMedPodSettings : ThingComp
    {
        private CompProperties_MedPodSettings Props
        {
            get
            {
                return (CompProperties_MedPodSettings)props;
            }
        }

        public float MaxDiagnosisTime
        {
            get
            {
                return Props.maxDiagnosisTime;
            }
        }

        public float MaxPerHediffHealingTime
        {
            get
            {
                return Props.maxPerHediffHealingTime;
            }
        }

        public float DiagnosisModePowerConsumption
        {
            get
            {
                return Props.diagnosisModePowerConsumption;
            }
        }

        public float HealingModePowerConsumption
        {
            get
            {
                return Props.healingModePowerConsumption;
            }
        }
    }
}
