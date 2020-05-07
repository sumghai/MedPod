using Verse;

namespace MedPod
{
    public class CompProperties_MedPodSettings : CompProperties
    {
        // Define defaults if not specified in XML defs

        public float maxDiagnosisTime = 5f;

        public float maxPerHediffHealingTime = 5f;

        public float diagnosisModePowerConsumption = 250f;

        public float healingModePowerConsumption = 750f;

        public CompProperties_MedPodSettings()
        {
            compClass = typeof(CompMedPodSettings);
        }
    }
}
