using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class DockMission : APMission {

        protected readonly ATClientModule mATC;
        
        BoxInfo BoxCurrent;
        bool needTarget = true;
        Vector3D drillStart;
        
        int emptyScans = 0;
        MyDetectedEntityInfo drillTarget;
        Action onUpdate;
        public DockMission(ModuleManager aManager) : base(aManager, default(BoundingSphereD)) {
            aManager.GetModule(out mATC);
            onUpdate = reserve;
        }

        void reserve() {
            mLog.log("reserve");
            mATC.ReserveDock();
            mThrust.Damp = true;
            if (mATC.Dock.isReserved) {
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
            var ms = mATC.Mother;
            var pos = mATC.Dock.theConnector * 2.5;
            Vector3D dir = Base6Directions.GetVector(mATC.Dock.ConnectorFace);
            return MAF.local2pos(pos + dir * (ms.Sphere.Radius * 0.5), ms.Matrix);
        }
        void dock() {
            mATC.ReserveDock();
            if (mATC.Dock.isReserved) {
                var ms = mATC.Mother;
                mThrust.Damp = false;
                Vector3D pos = mATC.Dock.theConnector * 2.5;
                Vector3D face = Base6Directions.GetVector(mATC.Dock.ConnectorFace);
                var mm = ms.Matrix;
                pos += face * MathHelperD.Clamp((mDistToDest) - 0.0, 2.5, 50.0);
                var veloAtPos = ms.VeloAt(pos);
                pos = MAF.local2pos(pos, mm);
                mDestination.Center = pos;
                mDestination.Radius = 0;
                mGyro.NavBlock = NavBlock = mATC.Connector;
                var dir = MAF.world2dir(mATC.Connector.WorldMatrix.Forward, mController.Remote.WorldMatrix);
                mGyro.SetTargetDirection(MAF.local2dir(-face, mm));
                BaseVelocity = ms.VeloDir * ms.Speed;
                BaseVelocity += veloAtPos;
                base.Update();
                mLog.log($"mDistToDest={mDistToDest}");
                if (mDistToDest < 1.0) {
                    mATC.Connector.Enabled = true;
                    mLog.log($"atc.Connector.Status={mATC.Connector.Status}");
                    if (mATC.Connector.Status == MyShipConnectorStatus.Connectable) {
                        mATC.Connector.Connect();
                        mThrust.Acceleration = Vector3D.Zero;
                        mGyro.SetTargetDirection(Vector3D.Zero);
                        return;
                    } else if (mATC.Connector.Status == MyShipConnectorStatus.Connected) {
                        mGyro.setGyrosEnabled(false);
                        var cargoLevel = mController.cargoLevel();
                        mLog.log($"cargoLevel={cargoLevel}");
                        if (cargoLevel == 0f) {
                            mLog.log("Completing mission");
                            mGyro.SetTargetDirection(Vector3D.Zero);
                            mGyro.NavBlock = null;
                            Complete = true;
                            return;
                        } else {
                            mLog.log("Draining cargo");
                        }
                        return;
                    }
                } else {
                    mATC.Connector.Enabled = false;
                }
                FlyTo(20.0);
            } else {
                onUpdate = reserve;
            }
        }
        void nothing() {
            BoxCurrent = mATC.GetBoxInfo(Volume.Center);
            mATC.ReserveCBox(BoxCurrent);
            if (BoxCurrent.IsReservedBy(mController.EntityId)) {

            } else {
                mThrust.Damp = true;
                mLog.log("Acquiring reservation ", BoxCurrent.Position);
            }
        }

        void scan() {
            MyDetectedEntityInfo entity;
            ThyDetectedEntityInfo thy;
            mCamera.Scan(mController.Volume.Center + MAF.ranDir() + 174.0, out entity, out thy);
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
                        drillStart = mController.Volume.Center;
                        mGyro.SetTargetDirection(Vector3D.Normalize(entity.HitPosition.Value - drillStart));
                    }
                }
            }
        }

    }
}
