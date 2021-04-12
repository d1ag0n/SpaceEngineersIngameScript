using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public  class OrbitMission : APMission {
        
        readonly List<IMyOreDetector> mDetectors = new List<IMyOreDetector>();

        int incrementsSince = 0;
        Vector3D orbit;
        Vector3D orbitAxis;
        MatrixD orbitMatrix;
        int orbitIncrements = 0;
        int matrixCalculations = 0;
        int updateIndex = 0;
        Action onScan;

        /// <summary>space to keep between us and thing</summary>
        double space => mController.Volume.Radius + PADDING + mEntity.WorldVolume.Radius;
        /// <summary>
        /// 
        /// </summary>
        Vector3D dirToThing => Vector3D.Normalize(mEntity.Position - mController.Volume.Center);
        /// <summary>position on the far side of the thing
        /// </summary>
        Vector3D orbitPos => mEntity.WorldVolume.Center + orbit * space;

        // this orbit is okayish I hope testing now
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking
        public OrbitMission(ModuleManager aManager, ThyDetectedEntityInfo aEntity) : base(aManager, aEntity) {
            orbit = Vector3D.Normalize(mController.Volume.Center - mEntity.Position);
            calculateOrbitMatrix();
            onScan = analyzeScan;
            mController.mManager.mProgram.GridTerminalSystem.GetBlocksOfType(mDetectors);
            var blackList = "Stone";
            foreach (var detector in mDetectors) {
                detector.SetValue("OreBlacklist", blackList);
            }
        }
        
        void calculateOrbitMatrix() {
            orbitIncrements = 0;
            orbit.CalculatePerpendicularVector(out orbitAxis);
            MatrixD.CreateFromAxisAngle(ref orbitAxis, -0.1, out orbitMatrix);
            Vector3D.Transform(orbitAxis, orbitMatrix);
            matrixCalculations++;
        }
        
        void incOrbit() {
            if (onScan == analyzeScan) {
                incrementsSince++;
                if (incrementsSince > 15) {
                    onScan = updateScan;
                }
            }
            orbitIncrements++;
            var tot = orbitIncrements * 0.1;
            if (tot > MathHelperD.TwoPi) {
                calculateOrbitMatrix();
            }
            Vector3D.TransformNormal(ref orbit, ref orbitMatrix, out orbit);
        }


        /// <summary>position that we want to fly at</summary>
        /// <returns></returns>
        Vector3D orbitProj() {
            var dir = dirToThing;
            return MAF.orthoProject(orbitPos, mEntity.WorldVolume.Center + -dir * space, dir);
        }
        
        public override void Update() {
            mThrust.Damp = false;
 
            var dest = orbitProj();
            var disp = dest - mController.Volume.Center;
            var dist = disp.LengthSquared();
            if (mDistToDest < 100) {
                mLog.log($"orbitIncrements={orbitIncrements}");
                mLog.log($"matrixCalculations={matrixCalculations}");
                var dir = disp;
                var mag = dir.Normalize();
                dir = MAF.world2dir(dir, mController.MyMatrix);
                var desiredVelo = dir * 5.0;
                mThrust.Acceleration = (desiredVelo - mController.LocalLinearVelo);
                mGyro.SetTargetDirection(mController.LinearVelocityDirection);
                mGyro.SetRollTarget(mEntity.Position);
                onScan();
                disp = orbitPos - mController.Volume.Center;
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
                if (mCamera.Scan(ore.Location, out entity, out thy)) {
                    var hit = entity.HitPosition.Value;
                    if (ore.BestApproach.IsZero()) {
                        ore.BestApproach = hit;
                    } else {
                        var curApp = (ore.BestApproach - ore.Location).LengthSquared();
                        var newApp = (ore.Location - hit).LengthSquared();
                        if (newApp < curApp) {
                            ore.BestApproach = hit;
                        }
                    }
                }
                
                var scanResult = oreScan(mEntity, ore.Location, out info, true);
                if (scanResult == 1) {
                    ATCModule atc;
                    if (mManager.GetModule(out atc)) {
                        atc.CancelDrill(ore.Index);
                    }
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
            var dir = MAF.ranDir() * (mEntity.WorldVolume.Radius * MAF.random.NextDouble());
            Vector3D scanPos;

            var dispToShip = wv.Center - mEntity.Position;
            
            var dot = dispToShip.Dot(dir);
            if (dot > 0) {
                scanPos = mEntity.Position + -dir;
            } else {
                scanPos = mEntity.Position + dir;
            }
            MyDetectedEntityInfo entity;
            ThyDetectedEntityInfo thy;
            mCamera.Scan(scanPos, out entity, out thy);
            if (thy != null && (thy.Type == ThyDetectedEntityType.Asteroid || thy.Type == ThyDetectedEntityType.AsteroidCluster)) {
                MyDetectedEntityInfo info;
                oreScan(thy, scanPos, out info, false);
            }
            scanPos = wv.Center + mController.LinearVelocityDirection * wv.Radius * 2.0;
            scanPos += MAF.ranDir() * wv.Radius * 2.0;
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
