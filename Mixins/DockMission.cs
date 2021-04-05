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
        Action onUpdate;
        public DockMission(ShipControllerModule aController, ATCLientModule aClient) : base(aController, default(BoundingSphereD)) {
            atc = aClient;
            onUpdate = reserve;
        }

        void reserve() {
            ctr.logger.log("reserve");
            atc.ReserveDock();
            ctr.Thrust.Damp = true;
            if (atc.Dock.isReserved) {
                onUpdate = dock;
            }
        }
        public override void Update() => onUpdate();

        /*void approach() {
            ctr.logger.log("Dock approach");
            atc.ReserveDock();
            if (atc.Dock.isReserved) {

                ctr.Thrust.Damp = false;
                var wv = ctr.Volume;
                mDestination = new BoundingSphereD(findApproach(), 0d);
                var disp = mDestination.Center - wv.Center;
                if (disp.LengthSquared() < wv.Radius * wv.Radius) {
                    onUpdate = dock;
                } else {
                    base.Update();
                    collisionDetectTo();
                }
            } else {
                onUpdate = reserve;
            }
        }*/
        Vector3D findApproach() {
            var pos = atc.Dock.theConnector * 2.5;
            Vector3D dir = Base6Directions.GetVector(atc.Dock.ConnectorFace);
            return MAF.local2pos(pos + dir * (ctr.MotherSphere.Radius * 0.5), ctr.MotherMatrix);
        }
        void dock() {
            atc.ReserveDock();
            if (atc.Dock.isReserved) {
                
                ctr.Thrust.Damp = false;
                Vector3D pos = atc.Dock.theConnector * 2.5;
                Vector3D face = Base6Directions.GetVector(atc.Dock.ConnectorFace);
                var mm = ctr.MotherMatrix;
                pos += face * MathHelperD.Clamp((mDistToDest) - 0.0, 2.5, 50.0);
                var veloAtPos = ctr.MotherVeloAt(pos);
                pos = MAF.local2pos(pos, mm);
                mDestination.Center = pos;
                mDestination.Radius = 0;
                ctr.Gyro.NavBlock = NavBlock = atc.Connector;
                var dir = MAF.world2dir(atc.Connector.WorldMatrix.Forward, ctr.Remote.WorldMatrix);
                ctr.Gyro.SetTargetDirection(MAF.local2dir(-face, mm));
                BaseVelocity = ctr.MotherVeloDir * ctr.MotherSpeed;
                BaseVelocity += veloAtPos;
                base.Update();
                if (mDistToDest < 1.0) {
                    atc.Connector.Enabled = true;
                    if (atc.Connector.Status == MyShipConnectorStatus.Connectable) {
                        atc.Connector.Connect();
                        ctr.Thrust.Acceleration = Vector3D.Zero;
                        ctr.Gyro.SetTargetDirection(Vector3D.Zero);
                        return;
                    } else if (atc.Connector.Status == MyShipConnectorStatus.Connected) {
                        ctr.Gyro.setGyrosEnabled(false);
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
                onUpdate = reserve;
            }
        }
        void nothing() {
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
