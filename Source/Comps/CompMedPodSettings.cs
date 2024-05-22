using UnityEngine;
using Verse;

namespace MedPod
{
    public class CompMedPodSettings : ThingComp
    {
        public CompProperties_MedPodSettings Props
        {
            get
            {
                return (CompProperties_MedPodSettings)props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Building_BedMedPod building_medpod = parent as Building_BedMedPod;

            if (building_medpod.powerComp.PowerOn && Props.screenGlowGraphicData != null)
            {
                Mesh screenGlowMesh = Props.screenGlowGraphicData.Graphic.MeshAt(parent.Rotation);

                Vector3 screenGlowDrawPos = parent.DrawPos;

                screenGlowDrawPos.y = AltitudeLayer.Building.AltitudeFor() + 0.03f;

                Graphics.DrawMesh(screenGlowMesh, screenGlowDrawPos + Props.screenGlowGraphicData.drawOffset.RotatedBy(parent.Rotation), Quaternion.identity, FadedMaterialPool.FadedVersionOf(Props.screenGlowGraphicData.Graphic.MatAt(parent.Rotation, null), 1), 0);
            }
        }
    }
}
