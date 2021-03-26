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
            atc.Connector.Enabled = false;
        }



        public override void Update() {
            

            if (atc.Dock.isReserved) {
                ctr.Damp = false;
                //ctr.logger.log("atc.Dock.Connector", atc.Dock.Connector);
                //ctr.logger.log("ctr.MotherMatrix", ctr.MotherMatrix);
                Vector3D pos = atc.Dock.Connector * 2.5;
                Vector3D face = Base6Directions.GetVector(atc.Dock.ConnectorFace);
                
                
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                //if (ctr.Camera.Scan(ctr.MotherSphere.Center, out entity, out thy, 10.0)) {
                //ctr.logger.log("orientation.translation", entity.Orientation.Translation);

                //}

                //var conPos = MAF.local2pos(pos, ctr.MotherMatrix);
                pos += face * (1.0 + mDistToDest * 0.4);
                pos = MAF.local2pos(pos, ctr.MotherMatrix);
                //ctr.logger.log("pos", pos);
                //ctr.logger.log("atc.Dock.ConnectorFace", atc.Dock.ConnectorFace);
                //ctr.logger.log("ctr.MotherSphere.Radius", ctr.MotherSphere.Radius);
                
                mDestination.Center = pos;
                //ctr.logger.log(ctr.logger.gps("pos", pos));
                mDestination.Radius = 0;
                ctr.Gyro.NavBlock = NavBlock = atc.Connector;
                
                var dir = MAF.world2dir(atc.Connector.WorldMatrix.Forward, ctr.Remote.WorldMatrix);
                ctr.Gyro.SetTargetDirection(MAF.local2dir(-face, ctr.MotherMatrix));
                //BaseVelocity = ctr.MotherVeloDir * ctr.MotherSpeed;
                base.Update();
                if (mDistToDest < 10.0) {
                    atc.Connector.Enabled = true;
                    if (atc.Connector.Status == MyShipConnectorStatus.Connectable) {
                        atc.Dock.Reserved = MAF.Epoch;

                    }
                } else {
                    atc.Connector.Enabled = false;
                }

                FlyTo(30.0);
            } else {
                ctr.logger.log("reserving dock");
                atc.Reserve(new DockMsg());
                ctr.Damp = true;
            }
            return;
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
