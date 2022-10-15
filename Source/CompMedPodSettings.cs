using System.Collections.Generic;
using UnityEngine;
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

        public bool DisableInvisibleBlocker
        {
            get
            {
                return Props.disableInvisibleBlocker;
            }
        }

        public List<AnimalRestingDrawOffset> AnimalRestingDrawOffsets
        { 
            get
            {
                return Props.animalRestingDrawOffsets;
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
