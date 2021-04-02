using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class DockMission : MissionBase {
        
        readonly ATCLientModule atc;
        BoxInfo BoxCurrent;
        bool needTarget = true;
        Vector3D drillStart;
        
        int emptyScans = 0;
        MyDetectedEntityInfo drillTarget;
        public DockMission(ShipControllerModule aController, ATCLientModule aClient) : base(aController, default(BoundingSphereD)) {
            atc = aClient;
        }

        public override void Update() {

            atc.ReserveDock();
            if (atc.Dock.isReserved) {
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (ctr.Camera.Scan(ctr.MotherCoM, out entity, out thy)) {
                    if (entity.EntityId == ctr.MotherId) {
                        
                    } else {
                        ctr.logger.log("Mother not scanned");
                    }
                }
                ctr.Thrust.Damp = false;
                //ctr.logger.log("atc.Dock.Connector", atc.Dock.Connector);
                //ctr.logger.log("ctr.MotherMatrix", ctr.MotherMatrix);
                Vector3D pos = atc.Dock.theConnector * 2.5;
                Vector3D face = Base6Directions.GetVector(atc.Dock.ConnectorFace);
                
                
                //if (ctr.Camera.Scan(ctr.MotherSphere.Center, out entity, out thy, 10.0)) {
                //ctr.logger.log("orientation.translation", entity.Orientation.Translation);

                //}
                var mm = ctr.MotherMatrix;

                //var conPos = MAF.local2pos(pos, ctr.MotherMatrix);
                //pos += face * (3.0 + mDistToDest * 0.6);
                pos += face * MathHelperD.Clamp((mDistToDest) - 5.0, 2.5, 50.0);
                var veloAtPos = ctr.MotherVeloAt(pos);
                ctr.logger.log("veloAtPos", veloAtPos);
                pos = MAF.local2pos(pos, mm);
                //ctr.logger.log("pos", pos);
                //ctr.logger.log("atc.Dock.ConnectorFace", atc.Dock.ConnectorFace);
                //ctr.logger.log("ctr.MotherSphere.Radius", ctr.MotherSphere.Radius);
                
                mDestination.Center = pos;
                //ctr.logger.log(ctr.logger.gps("pos", pos));
                mDestination.Radius = 0;
                ctr.Gyro.NavBlock = NavBlock = atc.Connector;
                
                var dir = MAF.world2dir(atc.Connector.WorldMatrix.Forward, ctr.Remote.WorldMatrix);
                ctr.Gyro.SetTargetDirection(MAF.local2dir(-face, mm));
                BaseVelocity = ctr.MotherVeloDir * ctr.MotherSpeed;
                BaseVelocity += veloAtPos;
                base.Update();
                ctr.logger.log($"mDistToDest={mDistToDest}");
                if (mDistToDest < 1.0) {
                    atc.Connector.Enabled = true;
                    if (atc.Connector.Status == MyShipConnectorStatus.Connectable) {
                        atc.Connector.Connect();
                        ctr.Thrust.Acceleration = Vector3D.Zero;
                        ctr.Gyro.SetTargetDirection(Vector3D.Zero);
                        return;
                    } else if (atc.Connector.Status == MyShipConnectorStatus.Connected) {
                        if (ctr.cargoLevel() == 0d) {
                            ctr.Gyro.SetTargetDirection(Vector3D.Zero);
                            ctr.Gyro.NavBlock = null;
                            Complete = true;
                        }
                        return;
                    }
                } else {
                    atc.Connector.Enabled = false;
                }

                FlyTo(20.0);
            } else {
                ctr.logger.log("reserving dock");
                
                //ctr.Damp = true;
            }
            return;
            BoxCurrent = atc.GetBoxInfo(Volume.Center);
            atc.ReserveCBox(BoxCurrent);
            if (BoxCurrent.IsReservedBy(ctr.EntityId)) {
                
            } else {
                ctr.Thrust.Damp = true;
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
