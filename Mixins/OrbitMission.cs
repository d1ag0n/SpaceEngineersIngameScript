using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public  class OrbitMission : APMission {
        
        readonly List<IMyOreDetector> mDetectors = new List<IMyOreDetector>();

        readonly Vector3D mOriginalOrbit;
        readonly Vector3D mOriginalPerp;
        
        MatrixD mRotOrbit;
        Vector3D mOrbit;
        Vector3D mPerp;
        int updateIndex;
        int orbitIncrements;
        int incrementsSince;
        Action onScan;
        int mMatrixCalculations;

        // create a matrix for rotating the orbit
        // rotate the perpendicular 
        void calculateOrbitMatrix() {
            MatrixD m;
            orbitIncrements = 0;
            if (mMatrixCalculations > 0) {
                var axis = mOriginalOrbit.Cross(mOriginalPerp);
                var angle = mMatrixCalculations * (MathHelper.Pi / 9f);
                MathHelper.LimitRadians(ref angle);
                m = MatrixD.CreateFromAxisAngle(axis, angle);
                mOrbit = Vector3D.Rotate(mOriginalOrbit, m);
                mPerp = Vector3D.Rotate(mOriginalPerp, m);
            }
            mMatrixCalculations++;
            MatrixD.CreateFromAxisAngle(ref mPerp, -0.1, out mRotOrbit);
        }


        
        /// <summary>space to keep between us and thing</summary>
        double space => mEntity.WorldVolume.Radius + mController.Volume.Radius * 2d;

        Vector3D dirToThing => Vector3D.Normalize(mEntity.Position - mController.Volume.Center);

        /// <summary>position on the far side of the thing</summary>
        Vector3D mOrbitPos => mEntity.WorldVolume.Center + mOrbit * space;

        // this orbit is okayish I hope testing now
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking
        public OrbitMission(ModuleManager aManager, ThyDetectedEntityInfo aEntity) : base(aManager, aEntity) {
            mOrbit = mOriginalOrbit = Vector3D.Normalize(mController.Volume.Center - mEntity.Position);
            mOriginalOrbit.CalculatePerpendicularVector(out mOriginalPerp);
            mPerp = mOriginalPerp;
            
            calculateOrbitMatrix();
            onScan = analyzeScan;
            mController.mManager.mProgram.GridTerminalSystem.GetBlocksOfType(mDetectors);
            var blackList = "Stone";
            foreach (var detector in mDetectors) {
                detector.SetValue("OreBlacklist", blackList);
            }
        }
        
        void incOrbit() {
            if (onScan == analyzeScan) {
                incrementsSince++;
                if (incrementsSince > 5) {
                    onScan = updateScan;
                }
            }
            orbitIncrements++;
            if (orbitIncrements >= 62) {
                calculateOrbitMatrix();
            }
            Vector3D.TransformNormal(ref mOrbit, ref mRotOrbit, out mOrbit);
        }

        /// <summary>position that we want to fly at</summary>
        /// <returns></returns>
        Vector3D orbitProj() {
            var dir = dirToThing;
            return MAF.orthoProject(mOrbitPos, mEntity.WorldVolume.Center + -dir * space, dir);
        }
        readonly V3DLag lag = new V3DLag(48);
        public override void Update() {
            mThrust.Damp = false;
 
            var dest = orbitProj();
            var disp = dest - mController.Volume.Center;
            var dist = disp.LengthSquared();
            if (mDistToDest < 100) {
                mLog.log($"orbitIncrements={orbitIncrements}");
                mLog.log($"matrixCalculations={mMatrixCalculations}");
                var dir = disp;
                var mag = dir.Normalize();
                dir = MAF.world2dir(dir, mController.MyMatrix);
                var desiredVelo = dir * 5.0;
                mThrust.Acceleration = (desiredVelo - mController.LocalLinearVelo);
                
                mGyro.SetTargetDirection(lag.update(mController.LinearVelocityDirection));
                mGyro.SetRollTarget(mEntity.Position);
                onScan();
                disp = mOrbitPos - mController.Volume.Center;
                dist = disp.LengthSquared();
                if (dist < 10000) {
                    incOrbit();
                }
            } else {
                mLog.persist("Out of orbit");
                base.Update();
                collisionDetectTo();
            }
            //ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            //var ops = new OPS(Volume.Center, Volume.Radius, mController.Grid.WorldVolume.Center);
            //ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            //ctr.logger.log(ops);
            
        }
        
        void updateScan() {
            mLog.log($"updateScan - ore count {mEntity.mOres.Count}");
            if (mEntity.mOres.Count > updateIndex) {
                var ore = mEntity.mOres[updateIndex];
                MyDetectedEntityInfo info;
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                mLog.log(mLog.gps("Testing", ore.Location));
                if (mCamera.Scan(ore.Location, out entity, out thy)) {
                    if (entity.HitPosition.HasValue) {
                        var hit = entity.HitPosition.Value;
                        // todo DP of approach to make sure it's on the correct side
                        if (ore.BestApproach.IsZero()) {
                            ore.BestApproach = hit;
                            mEntity.mOres[updateIndex] = ore;
                            //mLog.log(mLog.gps("FirstApproach", hit));
                        } else {
                            var curApp = (ore.BestApproach - ore.Location).LengthSquared();
                            var newApp = (ore.Location - hit).LengthSquared();
                            if (newApp < curApp) {
                                ore.BestApproach = hit;
                                mEntity.mOres[updateIndex] = ore;
                                //mLog.persist(mLog.gps("NewApproach", hit));
                            } else {
                                //mLog.log(mLog.gps("OriginalApproach", ore.BestApproach));
                            }
                        }
                    } else {
                        //mLog.persist(mLog.gps("ExpectedHit", ore.Location));
                    }
                } else {
                    mLog.persist(mLog.gps("ExpectedSuccess", ore.Location));
                }
                
                var scanResult = oreScan(mEntity, ore.Location, out info, true);
                if (scanResult == 1) {
                    mEntity.mOres.RemoveAtFast(updateIndex);
                    mLog.persist(mLog.gps("removed", ore.Location));
                } else if (scanResult > 1) {
                    updateIndex++;
                }
            } else {
                incrementsSince = updateIndex = 0;
                onScan = analyzeScan;
            }
        }
        void analyzeScan() {
            mLog.log($"analyzeScan - ore count {mEntity.mOres.Count}");
            var wv = mController.Volume;
            var dir = MAF.ranDir() * 100d;
            var scanPos = mEntity.Position + dir;
            
            MyDetectedEntityInfo entity;
            ThyDetectedEntityInfo thy;
            mCamera.Scan(scanPos, out entity, out thy);
            if (thy != null && (thy.Type == ThyDetectedEntityType.Asteroid || thy.Type == ThyDetectedEntityType.AsteroidCluster)) {
                MyDetectedEntityInfo info;
                oreScan(thy, scanPos, out info, false);
            }
            scanPos = wv.Center + mController.LinearVelocityDirection * wv.Radius * 3.0;
            scanPos += MAF.ranDir() * wv.Radius * 1.5;
            mCamera.Scan(scanPos, out entity, out thy);
        }
        int oreScan(ThyDetectedEntityInfo thy, Vector3D aPos, out MyDetectedEntityInfo info, bool update) {
            int result = 0;
            info = default(MyDetectedEntityInfo);
            foreach (var detector in mDetectors) {
                var range = detector.GetValue<double>("AvailableScanRange");
                range *= range;
                var disp = detector.WorldMatrix.Translation - aPos;
                var dist = disp.LengthSquared();
                if (dist >= range) {
                    continue;
                }
                
                detector.SetValue("RaycastTarget", aPos);
                info = detector.GetValue<MyDetectedEntityInfo>(update ? "DirectResult" : "RaycastResult");

                if (info.TimeStamp != 0) {
                    result = 1;
                    if (info.Name != "") {
                        result = 2;
                        if (thy.AddOre(info)) {
                            result = 3;
                            mLog.persist($"New {info.Name} Deposit found!");
                        }
                    }
                    break;
                } else {
                    mLog.persist("ORE Timestamp Zero");
                }
                //detector.SetValue("ScanEpoch", 0L);
                //throw new Exception("shouldnt be able to write ScanEpoch");
            }
            return result;
        }
    }
}
