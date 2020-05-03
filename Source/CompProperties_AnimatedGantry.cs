using Verse;

namespace MedPod
{
    public class CompProperties_AnimatedGantry : CompProperties
    {
        public GraphicData gantryGraphicData;

        public GraphicData machineLidGraphicData;

        public CompProperties_AnimatedGantry()
        {
            compClass = typeof(CompAnimatedGantry);
        }
    }
}
