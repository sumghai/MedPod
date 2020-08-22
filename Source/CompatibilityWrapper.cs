using HarmonyLib;
using System.Reflection;
using Verse;

namespace MedPod
{
    // Wrapper class for compatibility with mods like Elite Bionics Framework

    public class CompatibilityWrapper
    {
        public static MethodInfo makeshiftEbfEndpoint = AccessTools.Method("EBF.VanillaExtender:GetMaxHealth");

        public static FastInvokeHandler myDelegate = null;

        public static float GetMaxHealth(BodyPartDef def, Pawn pawn, BodyPartRecord record)
        {
            if (makeshiftEbfEndpoint != null)
            {
                if (myDelegate == null)
                {
                    myDelegate = MethodInvoker.GetHandler(makeshiftEbfEndpoint);
                }    
                return (float)myDelegate(null, new object[] { def, pawn, record });
            }
            return def.GetMaxHealth(pawn);
        }
    }    
}
