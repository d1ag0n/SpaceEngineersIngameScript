using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace IngameScript {
    
    public class DrillMission : MissionBase {
        const float cargoPercent = 0.75f;
        const double drillSpeed = 0.05;
        readonly ATCLientModule mATC;
        readonly BoundingSphereD mMissionAsteroid;
        readonly Vector3D mMissionTarget;
        readonly Vector3D mMissionStart;
        readonly Vector3D mMissionDirection;

        bool mCancel = false;
        Action onUpdate;

        double deepestDepth;
        double lastDepth;
        double entranceDepth;
        float lastCargo;
        
        bool firstEntrance = true;

        readonly List<IMyShipDrill> mDrill = new List<IMyShipDrill>();

        public override void Update() => onUpdate();

        public DrillMission(ShipControllerModule aController, BoundingSphereD aAsteroid, Vector3D aTarget) : 
            base(aController, new BoundingSphereD(aTarget, 0d)) {

            ctr.GetModule(out mATC);

            mMissionAsteroid = aAsteroid;
            mMissionTarget = aTarget;
            mMissionDirection = mMissionAsteroid.Center - mMissionTarget;
            mMissionDirection.Normalize();
            mMissionStart = mMissionAsteroid.Center + -mMissionDirection * (mMissionAsteroid.Radius + ctr.Volume.Radius);
            ctr.logger.persist(ctr.logger.gps("MissionStart", mMissionStart));
            mATC.Connector.Enabled = false;
            

            ctr.mManager.getByType(mDrill);
            stopDrill();
            ctr.Gyro.SetTargetDirection(Vector3D.Zero);
            ctr.Thrust.Damp = false;

            onUpdate = approach;
            lastCargo = ctr.cargoLevel();
            onCancel = doCancel;
        }
        void doCancel() {
            mCancel = true;
        }

        void approach() {
            mATC.Connector.Enabled = false;
            var com = ctr.Remote.CenterOfMass;
            var norm = Vector3D.Normalize(com - mMissionAsteroid.Center);
            var plane = mMissionAsteroid.Center + norm * mMissionAsteroid.Radius;            
            mDestination = new BoundingSphereD(MAF.orthoProject(mMissionStart, plane, norm), 0);
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            base.Update();
            FlyTo();
            var r = ctr.Volume.Radius;
            if (mDistToDest < 1d) {
                onUpdate = enter;
            }
        }
        
        
        void enter() {
            if (mCancel) {
                stopDrill();
                onUpdate = extract;
            }
            ctr.logger.log("enter");
            if (firstEntrance) {
                var flResult = followLine(5.0);
                entranceDepth = flResult;
                ctr.Gyro.SetTargetDirection(mMissionDirection);
                var wv = ctr.Volume;
                var scanPos = wv.Center + mMissionDirection * wv.Radius * 2.0;
                scanPos += MAF.ranDir() * wv.Radius;
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                ctr.Camera.Scan(scanPos, out entity, out thy);
                if (entity.Type == MyDetectedEntityType.Asteroid) {
                    var ct = wv.Contains(entity.HitPosition.Value);
                    var disp = wv.Center - entity.HitPosition.Value;
                    var dist = disp.LengthSquared();
                    if (dist < (wv.Radius * wv.Radius) + 500d) {
                        startDrill();
                        onUpdate = drill;
                        firstEntrance = false;
                    }
                }
            } else {
                var flResult = followLine(10.0);
                if (flResult + 20d > entranceDepth) {
                    startDrill();
                    onUpdate = drill;
                } else if (flResult + 25d > entranceDepth) {
                    ctr.Gyro.SetTargetDirection(mMissionDirection);
                }
            }
            //lastDepth = dlResult;
        }
        bool slow;
        void drill() {
            if (mCancel) {
                stopDrill();
                onUpdate = extract;
            }
            var speed = 2.5;
            if (lastDepth + 1.0 > deepestDepth) {
                speed = drillSpeed;
            }
            var cargo = ctr.cargoLevel();

            if (!slow && lastCargo == cargo) {
                speed = 0.5;
            } else {
                slow = true;
            }
            lastCargo = cargo;

            if (lastDepth < entranceDepth) {
                speed = 5.0;
            }

            var dist = followLine(speed);

            if (cargo > cargoPercent) {
                stopDrill();
                onUpdate = extract;
                deepestDepth -= 1d;
                slow = false;
                return;
            } else {
                
            }
            if (dist > deepestDepth) {
                deepestDepth = dist;
            }
            lastDepth = dist;
        }
        
        void extract() {

            mDestination = new BoundingSphereD(mMissionStart, 0);
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            base.Update();
            FlyTo();
            if (mDistToDest < 1d) {
                onUpdate = alignDock;
            }
        }
        void alignDock() {
            mATC.ReserveDock();
            if (mATC.Dock.isReserved) {
                ctr.Thrust.Damp = false;
                var com = ctr.Remote.CenterOfMass;
                var dockPos = MAF.local2pos(
                    (mATC.Dock.theConnector * 2.5) + mATC.Dock.ConnectorDir * (ctr.MotherSphere.Radius * 0.5), ctr.MotherMatrix
                    );
                var dir = Vector3D.Normalize(com - mMissionAsteroid.Center);
                var plane = mMissionAsteroid.Center + dir * mMissionAsteroid.Radius;
                var targetProjection = MAF.orthoProject(dockPos, plane, dir);
                if ((mMissionAsteroid.Center - com).LengthSquared() > (mMissionAsteroid.Radius + ctr.Volume.Radius) * (mMissionAsteroid.Radius + ctr.Volume.Radius)) {
                    mDestination = new BoundingSphereD(mMissionAsteroid.Center, 0);
                } else {
                    mDestination = new BoundingSphereD(targetProjection, 0);
                }
                base.Update();
                FlyTo();
                //ctr.logger.log(ctr.logger.gps("targetProjection", targetProjection));
                //var targetLocal = MAF.world2pos(targetProjection, ctr.MyMatrix);
                //var len = targetLocal.Normalize();
                //var velo = targetLocal * 8d;
                //var accel = velo - ctr.LocalLinearVelo;
                //ctr.Thrust.Acceleration = accel * 6d;
                //ctr.logger.log($"len={len}");
                // todo measure dist to dockPos
                if (mDistToDest < ctr.MotherSphere.Radius * 2d) {
                    onUpdate = approach;
                    if (mCancel) {
                        ctr.NewMission(new DockMission(ctr, mATC));
                    } else {
                        ctr.ExtendMission(new DockMission(ctr, mATC));
                    }
                }
            } else {
                ctr.Thrust.Damp = true;
            }
        }
        void undock() {
            mATC.Connector.Enabled = false;
            onUpdate = enter;
        }
        

        double followLine(double aSpeed = drillSpeed, bool reverse = false) {
            
            var r = ctr.Remote;
            var com = r.CenterOfMass;
            var disp = com - mMissionStart;
            var dir = disp;
            var dist = dir.Normalize() + 1.0;

            Vector3D pos;

            if (reverse) {
                pos = mMissionStart;
            } else {
                pos = mMissionStart + mMissionDirection * (dist + 20d);
            }
            
            var m = ctr.MyMatrix;
            pos = MAF.world2pos(pos, m);
            dir = pos;
            dir.Normalize();
            dir *= aSpeed;
            ctr.Thrust.Acceleration = dir - ctr.LocalLinearVelo;
            return dist;
        }
        void startDrill() {
            foreach (var d in mDrill) {
                d.Enabled = true;
            }
            ctr.Gyro.Roll = 0.2f;
        }
        void stopDrill() {
            foreach (var d in mDrill) {
                d.Enabled = false;
            }
            ctr.Gyro.Roll = 0f;
        }
    }
}
