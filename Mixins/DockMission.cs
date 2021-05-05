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
        protected readonly GyroModule mGyro;

        BoxInfo BoxCurrent;
        bool needTarget = true;
        Vector3D drillStart;
        
        int emptyScans = 0;
        MyDetectedEntityInfo drillTarget;
        Action onUpdate;
        public DockMission(ModuleManager aManager) : base(aManager, default(BoundingSphereD)) {
            aManager.GetModule(out mATC);
            aManager.GetModule(out mGyro);
            mGyro.MaxNGVelo = 0;
            onUpdate = align;
        }

        void align() {
            mATC.ReserveDock();
            if (mATC.Dock.isReserved) {
                var ms = mATC.Mother;
                Vector3D pos = mATC.Dock.theConnector * 2.5;
                Vector3D face = Base6Directions.GetVector(mATC.Dock.ConnectorFace);
                var mm = ms.Matrix;
                pos += face * ms.Sphere.Radius;
                mDestination.Center = MAF.local2pos(pos, ms.Matrix);
                BaseVelocity = ms.VeloDir * ms.Speed;
                pos = MAF.local2pos(pos, mm);
                base.Update();
                if (mDistToDest < mController.Volume.Radius) {
                    onUpdate = dock;
                } else {
                    collisionDetectTo();
                }
            } else {
                mThrust.Damp = true;
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
                pos += face * MathHelperD.Clamp((mDistToDest) - 0.0, 2.0, 50.0);
                var veloAtPos = ms.VeloAt(pos);
                pos = MAF.local2pos(pos, mm);
                mDestination.Center = pos;
                mDestination.Radius = 0;
                mGyro.NavBlock = NavBlock = mATC.mConnector;
                var dir = MAF.world2dir(mATC.mConnector.WorldMatrix.Forward, mController.Remote.WorldMatrix);
                mGyro.SetTargetDirection(MAF.local2dir(-face, mm));
                BaseVelocity = ms.VeloDir * ms.Speed;
                BaseVelocity += veloAtPos;
                base.Update();
                mLog.log($"mDistToDest={mDistToDest}");
                if (mDistToDest < 1.0) {
                    mATC.Connect();
                    if (mATC.connected) {
                        mThrust.Acceleration = Vector3D.Zero;
                        mGyro.SetTargetDirection(Vector3D.Zero);
                        mGyro.setGyrosEnabled(false);
                        var cargoLevel = mController.cargoLevel();
                        mLog.log($"cargoLevel={cargoLevel}");
                        if (cargoLevel == 0f) {
                            mLog.log("Completing mission");                        
                            mGyro.NavBlock = null;
                            Complete = true;
                        } else {
                            mLog.log("Draining cargo");
                        }
                        return;
                    }
                }
                FlyTo(20.0);
            } else {
                onUpdate = align;
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
            var entity = new MyDetectedEntityInfo();
            ThyDetectedEntityInfo thy;
            var target = mController.Volume.Center + MAF.ranDir() + 174.0;
            mCamera.Scan(ref target, ref entity, out thy);
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
