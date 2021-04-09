using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;

namespace IngameScript {
    public  class OrbitMission : APMission {

        Vector3D orbit;
        Vector3D orbitAxis;
        MatrixD orbitMatrix;
        int orbitIncrements = 0;
        int matrixCalculations = 0;


        readonly List<IMyOreDetector> mDetectors = new List<IMyOreDetector>();

        int updateIndex = 0;
        
        Action onScan;

        // this orbit is okayish I hope testing now
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking
        public OrbitMission(ModuleManager aManager, ThyDetectedEntityInfo aEntity) : base(aManager) {
            
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
        int incrementsSince = 0;
        void incOrbit() {
            
            if (onScan == analyzeScan) {
                incrementsSince++;
                if (incrementsSince > 30) {
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

        Vector3D dirToThing => Vector3D.Normalize(mEntity.Position - mController.Volume.Center);
        // position that we want to fly at
        Vector3D orbitProj() {
            var dir = dirToThing;
            return MAF.orthoProject(orbitPos, mEntity.WorldVolume.Center + -dir * space, dir);
        }

        
        // space to keep between us and thing
        double space => mController.Volume.Radius + PADDING + mEntity.WorldVolume.Radius;

        // position on the far side of the thing
        Vector3D orbitPos =>
            mEntity.WorldVolume.Center + orbit * space;
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
                var scanResult = oreScan(mEntity, ore.Location, out info, true);
                if (scanResult == 1) {
                    ATCModule atc;
                    if (mManager.GetModule(out atc)) {
                        atc.CancelDrill(ore.Index);
                    }
                    mEntity.mOres.RemoveAtFast(updateIndex);
                    updateIndex--;

                    mLog.persist(mLog.gps("removed", ore.Location));
                } else if (scanResult == 0) {
                    mLog.persist("Ore update failed.");
                }
                updateIndex++;
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thy"></param>
        /// <param name="aPos"></param>
        /// <returns>0 = scan fail, 1 = scan empty, 2 = scan found ore, 3 ore is new</returns>
        int oreScan(ThyDetectedEntityInfo thy, Vector3D aPos, out MyDetectedEntityInfo info, bool update) {
            int result = 0;
            info = default(MyDetectedEntityInfo);
            foreach (var detector in mDetectors) {
                var range = detector.GetValue<double>("AvailableScanRange");
                range *= range;
                var disp = detector.WorldMatrix.Translation - aPos;
                var dist = disp.LengthSquared();
                if (dist + 5d > range) {
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
            if (result == 0) {
                mLog.persist("Ore Scan Failure");
            }
            return result;
        }
    }
}
