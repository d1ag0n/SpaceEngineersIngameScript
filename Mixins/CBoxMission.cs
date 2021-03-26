using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class CBoxMission : MissionBase {
        
        readonly ATCLientModule atc;
        BoxInfo BoxCurrent;
        bool needTarget = true;
        Vector3D drillStart;
        int emptyScans = 0;
        MyDetectedEntityInfo drillTarget;
        public CBoxMission(ShipControllerModule aController, ATCLientModule aClient, BoundingSphereD aSphere) : base(aController, aSphere) {
            atc = aClient;
        }



        public override void Update() {
            base.Update();
            BoxCurrent = atc.GetBoxInfo(Volume.Center);
            atc.Reserve(BoxCurrent);
            if (BoxCurrent.IsReservedBy(ctr.EntityId)) {
                
            } else {
                ctr.Damp = true;
                ctr.logger.log("Acquiring reservation ", BoxCurrent.Position);
            }
        }

        void scan() {
            MyDetectedEntityInfo entity;
            ThyDetectedEntityInfo thy;
            ctr.Camera.Scan(ctr.Volume.Center + MAF.ranDir() + 174.0, out entity, out thy);
            if (entity.EntityId == 0) {
                emptyScans++;
                if (emptyScans > 60) {
                    needTarget = true;
                }
            } else {
                emptyScans = 0;
                if (entity.Type == MyDetectedEntityType.Asteroid) {
                    if (needTarget) {
                        needTarget = false;
                        drillTarget = entity;
                        drillStart = ctr.Volume.Center;
                        ctr.Gyro.SetTargetDirection(Vector3D.Normalize(entity.HitPosition.Value - drillStart));
                    }
                }
            }
        }
    }
}
