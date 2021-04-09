using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace IngameScript {
    
    public class DrillMission : APMission {
        const float cargoPercent = 0.75f;
        const double drillSpeed = 0.06;

        readonly List<IMyShipDrill> mDrill = new List<IMyShipDrill>();

        readonly BoundingSphereD mMissionAsteroid;
        readonly Vector3D mMissionTarget;
        readonly Vector3D mMissionStart;
        readonly Vector3D mMissionDirection;
        readonly ATClientModule mATC;
        

        bool mCancel = false;
        Action onUpdate;
        double deepestDepth;
        double lastDepth;
        double entranceDepth;
        float lastCargo;
        bool firstEntrance = true;
        
        

        public override void Update() => onUpdate();

        
        

        //Mission = new DockMission(this, ATClient, Volume);

        public DrillMission(ModuleManager aManager, BoundingSphereD aAsteroid, Vector3D aTarget) : 
            base(aManager)
        {
            aManager.GetModule(out mATC);
            mLog = mATC.mLog;
            if (mATC.connected) {
                mThrust.Damp = false;
            }

            mMissionAsteroid = aAsteroid;
            mMissionTarget = aTarget;
            mMissionDirection = mMissionAsteroid.Center - mMissionTarget;
            mMissionDirection.Normalize();
            mMissionStart = mMissionAsteroid.Center + -mMissionDirection * (mMissionAsteroid.Radius + mController.Volume.Radius);
            mController.mLog.persist(mController.mLog.gps("MissionStart", mMissionStart));
            mATC.Connector.Enabled = false;
            

            mManager.getByType(mDrill);
            stopDrill();
            mGyro.SetTargetDirection(Vector3D.Zero);
            mThrust.Damp = false;

            onUpdate = approach;
            lastCargo = mController.cargoLevel();
            onCancel = doCancel;
        }
        void doCancel() {
            mCancel = true;
        }
        double AltitudeSq => (mMissionAsteroid.Center - mController.Remote.CenterOfMass).LengthSquared();
        double Altitude => Math.Sqrt(AltitudeSq);
        double MaxAltitudeSq => (mMissionAsteroid.Radius + mController.Volume.Radius) * (mMissionAsteroid.Radius + mController.Volume.Radius);
        double MaxAltitude => Math.Sqrt(MaxAltitudeSq);
        void approach() {
            mLog.log($"approach, Alt={Altitude}, Max={MaxAltitude}");
            if (mController.cargoLevel() > 0f) {
                onUpdate = alignDock;
            }
            mATC.Connector.Enabled = false;
            var com = mController.Remote.CenterOfMass;
            var norm = Vector3D.Normalize(com - mMissionAsteroid.Center);
            var plane = mMissionAsteroid.Center + norm * mMissionAsteroid.Radius;
            if (AltitudeSq > MaxAltitudeSq) {
                mDestination = new BoundingSphereD(MAF.orthoProject(mMissionAsteroid.Center, plane, norm), 0);
            } else {

                mDestination = new BoundingSphereD(MAF.orthoProject(mMissionStart, plane, norm), 0);
            }
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            base.Update();
            FlyTo(10d);
            var r = mController.Volume.Radius;
            if (mDistToDest < 1d) {
                onUpdate = enter;
            }
        }
        
        
        void enter() {
            mLog.log($"enter, Alt={Altitude}, Max={MaxAltitude}");
            if (mCancel) {
                stopDrill();
                onUpdate = extract;
            }
            if (firstEntrance) {
                var flResult = followLine(5.0);
                entranceDepth = flResult;
                mGyro.SetTargetDirection(mMissionDirection);
                var wv = mController.Volume;
                var scanPos = wv.Center + mMissionDirection * wv.Radius * 2.0;
                scanPos += MAF.ranDir() * wv.Radius;
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                mCamera.Scan(scanPos, out entity, out thy);
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
                    mGyro.SetTargetDirection(mMissionDirection);
                }
            }
            //lastDepth = dlResult;
        }
        bool slow;
        void drill() {
            mLog.log($"drilling, Alt={Altitude}, Max={MaxAltitude}");
            if (mCancel) {
                stopDrill();
                onUpdate = extract;
            }
            var speed = 2.5;
            if (lastDepth + 1.0 > deepestDepth) {
                speed = drillSpeed;
            }
            var cargo = mController.cargoLevel();

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
                
                onUpdate = extract;
                deepestDepth -= 1d;
                slow = false;
                return;
            }
            if (dist > deepestDepth) {
                deepestDepth = dist;
            }
            lastDepth = dist;
            var targDist = (mMissionTarget - mController.Remote.CenterOfMass).LengthSquared();
            mLog.log($"targDist={targDist}");
            if (targDist < 25d) {
                mCancel = true;
            }
        }
        
        void extract() {
            mLog.log($"extract, Alt={Altitude}, Max={MaxAltitude}");
            mDestination = new BoundingSphereD(mMissionStart, 0);
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            
            var flr = followLine(5.0, true);
            mLog.log($"flr={flr}, entranceDepth={entranceDepth}");
            if (flr < entranceDepth) {
                stopDrill();
            }
            if (flr < 50d) {
                onUpdate = alignDock;
            }
        }
        void alignDock() {
            mLog.log($"alignDock, Alt={Altitude}, Max={MaxAltitude}");
            mATC.ReserveDock();
            if (mATC.Dock.isReserved) {
                mThrust.Damp = false;
                var com = mController.Remote.CenterOfMass;
                var dockPos = MAF.local2pos(
                    (mATC.Dock.theConnector * 2.5) + mATC.Dock.ConnectorDir * (mController.Sphere.Radius * 0.5), mController.MotherMatrix
                    );
                var dir = Vector3D.Normalize(com - mMissionAsteroid.Center);
                var plane = mMissionAsteroid.Center + dir * MaxAltitude;
                var targetProjection = MAF.orthoProject(dockPos, plane, dir);
                if (AltitudeSq > MaxAltitudeSq) {
                    mDestination = new BoundingSphereD(mMissionAsteroid.Center, 0);
                } else {
                    mDestination = new BoundingSphereD(targetProjection, 0);
                }
                base.Update();
                FlyTo(10d);
                //ctr.logger.log(ctr.logger.gps("targetProjection", targetProjection));
                //var targetLocal = MAF.world2pos(targetProjection, ctr.MyMatrix);
                //var len = targetLocal.Normalize();
                //var velo = targetLocal * 8d;
                //var accel = velo - ctr.LocalLinearVelo;
                //ctr.Thrust.Acceleration = accel * 6d;
                //ctr.logger.log($"len={len}");
                // todo measure dist to dockPos
                if (mDistToDest < mController.MotherSphere.Radius * 2d) {
                    onUpdate = approach;
                    if (mCancel) {
                        mController.NewMission(new DockMission(mController, mATC));
                    } else {
                        mController.ExtendMission(new DockMission(mController, mATC));
                    }
                }
            } else {
                mThrust.Damp = true;
            }
        }
        void undock() {
            mATC.Connector.Enabled = false;
            onUpdate = enter;
        }
        

        double followLine(double aSpeed = drillSpeed, bool reverse = false) {
            
            var r = mController.Remote;
            var com = r.CenterOfMass;
            var disp = com - mMissionStart;
            var dir = disp;
            var dist = dir.Normalize() + 1.0;

            Vector3D pos;

            if (reverse) {
                pos = mMissionStart + mMissionDirection * (dist + -1d);
            } else {
                pos = mMissionStart + mMissionDirection * (dist + 1d);
            }
            
            var m = mController.MyMatrix;
            pos = MAF.world2pos(pos, m);
            dir = pos;
            dir.Normalize();
            dir *= aSpeed;
            mThrust.Acceleration = dir - mController.LocalLinearVelo;
            return dist;
        }
        void startDrill() {
            foreach (var d in mDrill) {
                d.Enabled = true;
            }
            mGyro.Roll = 0.2f;
        }
        void stopDrill() {
            foreach (var d in mDrill) {
                d.Enabled = false;
            }
            mGyro.Roll = 0f;
        }

    }
}
