using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    
    public class DrillMission : APMission {
        const float CARGO_PCT = 0.90f;
        const double DRILL_SPEED = 0.3;

        readonly List<IMyShipDrill> mDrill = new List<IMyShipDrill>();

        BoundingSphereD mMissionAsteroid;
        readonly Vector3D mMissionTarget;
        readonly Vector3D mMissionStart;
        //readonly Vector3D mMissionApproach;
        readonly Vector3D mMissionDirection;
        readonly ATClientModule mATC;
        readonly GyroModule mGyro;
        readonly OreDetectorModule mOre;
        readonly List<OreSearch> mSearch = new List<OreSearch>();
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
            aManager.GetModule(out mOre);
            mController.mManual = false;
            mThrust.Damp = false;
            mLog.persist("align dock damp to false");
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

            

            mManager.getByType(mDrill);
            
            mGyro.SetTargetDirection(Vector3D.Zero);

            
            if (mATC.connected) {
                stopDrill();
                onUpdate = approach;
            } else {
                startDrill();
                onUpdate = escape;
            }
            
            lastCargo = mController.cargoLevel();
            mEscape = mController.Remote.WorldMatrix.Forward;
            oreSearchMachine = oreSearch();
            oreSearchMachine.MoveNext();
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
            mThrust.Damp = false;
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

            if (mController.cargoLevel() > 0f) {
                onUpdate = alignDock;
                return;
            }

            var plane = orbitPlane(out dir, out dif);
            mThrust.Damp = false;
            mLog.log($"approach, dif={dif}");
            info();
            scanRoid();
            mATC.Disconnect();
            
            

            mDestination = new BoundingSphereD(MAF.orthoProject(mMissionStart, plane, dir), 0);
            
            //var disp = com - mMissionStart;
            //var distSq = disp.LengthSquared();
            base.Update();
            FlyTo(10d);
            var r = mController.Volume.Radius;
            if (mDistToDest < 25d) {
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
                scanPos += MAF.ranDir() * wv.Radius + 2.5;
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
                speed = DRILL_SPEED;
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

            if (cargo > CARGO_PCT) {
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
                onUpdate = search;
            }
            if (mController.ShipVelocities.AngularVelocity.LengthSquared() < 0.01) {
                ngRev++;
            } else {
                ngRev = 0;
            }
            if (ngRev > 12) {
                ngRev = 0;
                mGyro.Roll = -mGyro.Roll;
            }
        }
        Vector3D mMovePosition;
        Vector3D mMoveCurrentPosition;
        Vector3D mMoveDirection;
        Vector3D mMoveCurrentDirection;
        MatrixD mMoveMatrix;
        void move(double aVelo) {
            var com = mController.Remote.CenterOfMass;
            var f = mController.Remote.WorldMatrix.Forward;
            var canMove = false;
            if (MAF.angleBetween(mMoveDirection, f) < 0.1) {
                mMoveCurrentDirection = mMoveDirection;
                canMove = true;
            } else {
                if (MAF.angleBetween(f, mMoveCurrentDirection) < 0.1) {
                    Vector3D.Transform(ref mMoveCurrentDirection, ref mMoveMatrix, out mMoveCurrentDirection);
                }
            }
            mGyro.SetTargetDirection(mMoveCurrentDirection);
            if (canMove) {
                if (Vector3D.DistanceSquared(com, mMovePosition) < 1d) {
                    mMoveCurrentPosition = mMovePosition;
                } else {
                    if (Vector3D.DistanceSquared(com, mMoveCurrentPosition) < 1d) {
                        mMoveCurrentPosition = com + mMoveDirection * 2d;
                    }
                }
            }
            var velo = MathHelperD.Clamp(Vector3D.Distance(com, mMoveCurrentPosition), 0, aVelo);
        }
        void setMove(Vector3D aTarget) {
            var com = mController.Remote.CenterOfMass;
            
            mMovePosition = aTarget;
            mMoveDirection = Vector3D.Normalize(aTarget - com);
            
            if (Vector3D.DistanceSquared(com, aTarget) < 6.25) {
                mMoveCurrentPosition = aTarget;
            } else {
                mMoveCurrentPosition = com + mMoveDirection * 2d;
            }
            
            mMoveMatrix = MatrixD.CreateFromAxisAngle(mMoveCurrentDirection.Cross(mMoveDirection), 0.1);
            var f = mController.Remote.WorldMatrix.Forward;
            if (MAF.angleBetween(mMoveDirection, f) < 0.1) {
                mMoveCurrentDirection = f;
            } else {
                Vector3D.TransformNormal(ref f, ref mMoveMatrix, out mMoveCurrentDirection);
            }
        }
        void search() {
        }

        class OreSearch {
            public readonly string mName;
            public readonly Vector3D mStartPosition;
            public readonly Vector3D mStartDirection;
            public readonly Vector3D mOrePosition;
            public OreSearch(string aName, Vector3D aStartPosition, Vector3D aStartDirection, Vector3D aOrePosition) {
                mName = aName;
                mStartPosition = aStartPosition;
                mStartDirection = aStartDirection;
                mOrePosition = aOrePosition;
            }

        }
        OreSearch mSearchResult;
        readonly IEnumerator<bool> oreSearchMachine;
        IEnumerator<bool> oreSearch() {
            yield return true;
            Vector3D originalDir, dir, CoM, target;
            MatrixD roll, yaw;
            double rolls, range;
            ThyDetectedEntityInfo thy;
            MyDetectedEntityInfo entity = new MyDetectedEntityInfo();
            RayD ray;
            double? intersect;
            BoundingSphereD back;
            double angle = MathHelperD.TwoPi / 64d;
            int oreScan;
            while (true) {
                CoM = mController.Remote.CenterOfMass;
                back = new BoundingSphereD(CoM + mController.Remote.WorldMatrix.Backward * (mController.Volume.Radius * 2d), mController.Volume.Radius);
                originalDir = mController.Remote.WorldMatrix.Forward;
                roll = MatrixD.CreateFromAxisAngle(originalDir, angle);
                yaw = MatrixD.CreateFromAxisAngle(mController.Remote.WorldMatrix.Right, angle);
                rolls = 0;
                range = mController.Volume.Radius + 10d;
                Vector3D.TransformNormal(ref originalDir, ref yaw, out dir);

                yield return true;
                while (true) {
                    target = CoM + dir * range;
                    ray = new RayD(CoM, dir);
                    intersect = back.Intersects(ray);
                    if (intersect.HasValue) {
                        break;
                    }
                    if (mCamera.Scan(ref target, ref entity, out thy)) {
                        if (entity.HitPosition.HasValue) {
                            if (entity.Type == MyDetectedEntityType.Asteroid) {
                                while (0 == (oreScan = mOre.Scan(null, target, out entity, false))) {
                                    yield return true;
                                }
                                if (oreScan == 1 || !entity.HitPosition.HasValue) {
                                    // nothing
                                } else {
                                    mSearchResult = new OreSearch(entity.Name, CoM, originalDir, entity.HitPosition.Value);
                                    break;
                                }
                            }
                        } else {
                            range += 10;
                        }
                    }
                    if (rolls >= MathHelperD.TwoPi) {
                        rolls = 0;
                        Vector3D.TransformNormal(ref dir, ref yaw, out dir);
                    } else {
                        rolls += angle;
                        Vector3D.TransformNormal(ref dir, ref roll, out dir);
                    }
                }
                yield return false;
            }
        }
        int ngRev = 0;
        
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
                mLog.persist("align dock DAMP TO false");
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
                mLog.persist("align dock DAMP TO TRUE");
                
            }
        }
        void undock() {
            mATC.Disconnect();
            onUpdate = enter;
        }
        

        double followLine(double aSpeed = DRILL_SPEED, bool reverse = false) {
            Vector3D pos;

            var com = mController.Remote.CenterOfMass;
            var start2com = com - mMissionStart;
            var dir2com = start2com;
            var dist2com = dir2com.Normalize();

            var position = mMissionStart + mMissionDirection * dist2com;

            var dispToPos = position - com;
            var dirToPos = dispToPos;
            var off = dirToPos.Normalize();



            if (off > 0.25 && !reverse) {
                dist2com -= off * 0.5;
            } else {
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
