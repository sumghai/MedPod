using Verse;

namespace MedPod
{
    public class CompProperties_AnimatedGantry : CompProperties
    {
        // Define defaults if not specified in XML defs

        public GraphicData gantryGraphicData;

        public GraphicData gantryGlowGraphicData;

        public GraphicData machineLidGraphicData = null;

        public float gantryMaxMoveDistance = 1.71875f; // 1.71875 = 110 px max gantry offset

        public CompProperties_AnimatedGantry()
        {
            compClass = typeof(CompAnimatedGantry);
        }
    }
}
