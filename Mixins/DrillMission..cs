using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    
    public class DrillMission : APMission {
        const float cargoPercent = 0.90f;
        const double drillSpeed = 0.2;

        readonly List<IMyShipDrill> mDrill = new List<IMyShipDrill>();

        BoundingSphereD mMissionAsteroid;
        readonly Vector3D mMissionTarget;
        readonly Vector3D mMissionStart;
        //readonly Vector3D mMissionApproach;
        readonly Vector3D mMissionDirection;
        readonly ATClientModule mATC;
        readonly GyroModule mGyro;

        bool mCancel = false;
        Action onUpdate;
        double deepestDepth;
        double lastDepth;
        double entranceDepth;
        float lastCargo;
        bool firstEntrance = true;
        
        

        public override void Update() => onUpdate();


        

        //Mission = new DockMission(this, ATClient, Volume);

        public DrillMission(ModuleManager aManager, BoundingSphereD aAsteroid, Vector3D aTarget, Vector3D aBestApproach) : base(aManager, aAsteroid) {
            aManager.GetModule(out mATC);
            aManager.GetModule(out mGyro);

            if (mATC.connected) {
                mThrust.Damp = false;
            }
            mMissionAsteroid = aAsteroid;
            mMissionTarget = aTarget;
            var disp2center = mMissionAsteroid.Center - mMissionTarget;
            /*var disp2target = mMissionTarget - aBestApproach;
            if (aBestApproach.IsZero() || disp2center.Dot(disp2target) < 0) {
                
            } else {
                mMissionDirection = disp2target;
            }*/
            mMissionDirection = disp2center;
            mMissionDirection.Normalize();
            /*if (aBestApproach.IsZero()) {
                
            } else {
                mMissionStart = mMissionTarget + -mMissionDirection * (mMissionAsteroid.Radius + mController.Volume.Radius);
            }*/
            mMissionStart = mMissionAsteroid.Center + -mMissionDirection * (mMissionAsteroid.Radius + mController.Volume.Radius);
            mATC.Disconnect();
            

            mManager.getByType(mDrill);
            
            mGyro.SetTargetDirection(Vector3D.Zero);
            mThrust.Damp = false;
            if (mATC.connected) {
                stopDrill();
                onUpdate = approach;
            } else {
                startDrill();
                onUpdate = escape;
            }
            lastCargo = mController.cargoLevel();
            mEscape = mController.Remote.WorldMatrix.Forward;
        }
        public override bool Cancel() {
            mCancel = true;
            return false;
        }
        Vector3D orbitPlane(out Vector3D dir, out double dif) {
            var disp2ship = mController.Volume.Center - mMissionAsteroid.Center;
            dir = disp2ship;
            var shipAltitude = dir.Normalize();
            var oa = mMissionAsteroid.Radius + mATC.Mother.Sphere.Radius;
            dif = oa - shipAltitude;
            return mMissionAsteroid.Center + dir * (oa + dif);
        }
        /*double AltitudeSq => (mMissionAsteroid.Center - mController.Volume.Center).LengthSquared();
        double Altitude => Math.Sqrt(AltitudeSq);
        double MaxAltitudeSq => (mMissionAsteroid.Radius + mController.Volume.Radius) * (mMissionAsteroid.Radius + mController.Volume.Radius);
        double MaxAltitude => Math.Sqrt(MaxAltitudeSq);*/
        void scanRoid() {
            var entity = new MyDetectedEntityInfo();
            ThyDetectedEntityInfo thy;
            if (mCamera.Scan(ref mMissionAsteroid.Center, ref entity, out thy)) {
                if (entity.Type == MyDetectedEntityType.Asteroid) {
                    mMissionAsteroid = mMissionAsteroid.Include(new BoundingSphereD(entity.HitPosition.Value, 10d));
                }
            }
        }
        Vector3D mEscape;
        void escape() {
            double dif;
            Vector3D dir;
            orbitPlane(out dir, out dif);
            mLog.log($"escape, dif={dif}");
            info();
            scanRoid();

 
            if (dif > mController.Volume.Radius) {
                mGyro.SetTargetDirection(mEscape);
                mThrust.Acceleration = (MAF.world2dir(mController.Remote.WorldMatrix.Backward, mController.MyMatrix) * 4d) - mController.LocalLinearVelo;
            } else {
                stopDrill();
                onUpdate = approach;
            }
        }
        void approach() {
            Vector3D dir;
            double dif;
            var plane = orbitPlane(out dir, out dif);
            mLog.log($"approach, dif={dif}");
            info();
            scanRoid();
            
            if (mController.cargoLevel() > 0f) {
                onUpdate = alignDock;
                return;
            }
            
            
            
            
            
            

            mDestination = new BoundingSphereD(MAF.orthoProject(mMissionStart, plane, dir), 0);
            
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            base.Update();
            FlyTo(10d);
            var r = mController.Volume.Radius;
            if (mDistToDest < 1d) {
                onUpdate = enter;
            }
        }

        void info() {
            mLog.log(mLog.gps("mMissionStart", mMissionStart));
            mLog.log(mLog.gps("mMissionTarget", mMissionTarget));
        }
        
        void enter() {
            mLog.log($"enter");
            info();
            if (mCancel) {
                stopDrill();
                onUpdate = extract;
            }
            mGyro.SetTargetDirection(mMissionDirection);
            if (firstEntrance) {
                var flResult = followLine(4.0);
                entranceDepth = flResult;
                
                var wv = mController.Volume;
                var scanPos = wv.Center + mMissionDirection * wv.Radius * 2.0;
                scanPos += MAF.ranDir() * wv.Radius;
                var entity = new MyDetectedEntityInfo();
                ThyDetectedEntityInfo thy;
                mCamera.Scan(ref scanPos, ref entity, out thy);
                if (entity.Type == MyDetectedEntityType.Asteroid) {
                    //var ct = wv.Contains(entity.HitPosition.Value);
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
                }
            }
            //lastDepth = dlResult;
        }
        bool slow;
        void drill() {
            mLog.log($"drilling");
            info();
            if (mCancel) {
                stopDrill();
                onUpdate = extract;
            }
            var speed = 2.5;
            if (lastDepth + 2.5 > deepestDepth) {
                speed = drillSpeed;
            }
            var cargo = mController.cargoLevel();

            if (!slow && lastCargo == cargo) {
                //speed = 0.5;
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
            var targDist = (mMissionTarget - mController.Volume.Center).LengthSquared();
            mLog.log($"targDist={targDist}");
            if (targDist < 5d) {
                mCancel = true;
            }
        }
        
        void extract() {
            mLog.log($"extract");
            info();
            mDestination = new BoundingSphereD(mMissionStart, 0);
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            
            var flr = followLine(5.0, true);
            mLog.log($"flr={flr}, entranceDepth={entranceDepth}");
            if (flr < entranceDepth) {
                stopDrill();
            }
            if (flr < mController.Volume.Radius * 2d) {
                onUpdate = alignDock;
                lastCargo = mController.cargoLevel();
            }
        }
        void alignDock() {
            mLog.log($"alignDock");
            info();
            scanRoid();
            mATC.ReserveDock();
            if (mATC.Dock.isReserved) {
                mThrust.Damp = false;
                var wv = mController.Volume;
                var com = mController.Remote.CenterOfMass;
                var ms = mATC.Mother;
                var dockPos = MAF.local2pos((mATC.Dock.theConnector * 2.5) + mATC.Dock.ConnectorDir * ms.Sphere.Radius, ms.Matrix);
                Vector3D dir;
                double dif;
                var plane = orbitPlane(out dir, out dif);
                var targetProjection = MAF.orthoProject(dockPos, plane, dir);
                
                mDestination = new BoundingSphereD(targetProjection, 0);
                BaseVelocity = ms.VeloDir * ms.Speed;
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
                
                
                var dispToProj = (targetProjection - mMissionAsteroid.Center);
                var dispToDock = (dockPos - mMissionAsteroid.Center);
                var dot = dispToProj.Dot(dispToDock);
                mLog.log($"dot={dot}, mDistToDest={mDistToDest}, wv.Radius={wv.Radius}");
                if (dot > 0d) {
                    if (mDistToDest < wv.Radius * 2d) {
                        onUpdate = approach;
                        if (mCancel) {
                            mController.NewMission(new DockMission(mManager));
                        } else {
                            mController.ExtendMission(new DockMission(mManager));
                        }
                    }
                }
            } else {
                mThrust.Damp = true;
            }
        }
        void undock() {
            mATC.Disconnect();
            onUpdate = enter;
        }
        

        double followLine(double aSpeed = drillSpeed, bool reverse = false) {
            Vector3D pos;

            var com = mController.Remote.CenterOfMass;
            var start2com = com - mMissionStart;
            var dir2com = start2com;
            var dist2com = dir2com.Normalize();

            var position = mMissionStart + mMissionDirection * dist2com;

            if (Vector3D.DistanceSquared(com, position) < 0.625) {
                dist2com += 2.5;
            }


            if (reverse) {
                pos = mMissionStart + mMissionDirection * -dist2com;
            } else {
                pos = mMissionStart + mMissionDirection * dist2com;
            }
            
            var m = mController.MyMatrix;
            pos = MAF.world2pos(pos, m);
            var dir = pos;
            dir.Normalize();
            dir *= aSpeed;
            mThrust.Acceleration = dir - mController.LocalLinearVelo;
            return dist2com;
        }
        public void startDrill() {
            foreach (var d in mDrill) {
                d.Enabled = true;
            }
            mGyro.Roll = 0.19f;
        }
        public void stopDrill() {
            foreach (var d in mDrill) {
                d.Enabled = false;
            }
            mGyro.Roll = 0f;
        }

    }
}
