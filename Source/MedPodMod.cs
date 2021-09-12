using HarmonyLib;
using Verse;
using Verse.AI;

namespace MedPod
{
    public class MedPodMod : Mod
    {
        public MedPodMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.MedPod.patches");
            harmony.PatchAll();

            if (ModCompatibility.DbhIsActive)
            {
                Log.Message("MedPod :: Dubs Bad Hygiene detected!");
            }
        }
    }
}