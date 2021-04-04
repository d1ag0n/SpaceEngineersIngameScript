using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;

namespace IngameScript {
    public  class OrbitMission : MissionBase {
        Vector3D orbit;
        Vector3D orbitAxis;
        MatrixD orbitMatrix;
        int orbitIncrements = 0;
        int matrixCalculations = 0;

        readonly List<IMyOreDetector> mDetectors = new List<IMyOreDetector>();
        int updateIndex = 0;
        bool update = false;

        // this orbit is okayish I hope testing now
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking
        public OrbitMission(ShipControllerModule aController, ThyDetectedEntityInfo aEntity) : base(aController, aEntity) {
            orbit = Vector3D.Normalize(ctr.Volume.Center - mEntity.Position);
            calculateOrbitMatrix();
            ctr.mManager.mProgram.GridTerminalSystem.GetBlocksOfType(mDetectors);
            var blackList = "Stone";
            foreach (var detector in mDetectors) {
                detector.SetValue("OreBlacklist", blackList);
            }
            onScan = analyzeScan;
        }
        
        void calculateOrbitMatrix() {
            if (!update && matrixCalculations * 0.1 > MathHelperD.TwoPi) {
                update = true;
                onScan = updateScan;
            }
            orbitIncrements = 0;
            orbit.CalculatePerpendicularVector(out orbitAxis);
            MatrixD.CreateFromAxisAngle(ref orbitAxis, -0.1, out orbitMatrix);
            Vector3D.Transform(orbitAxis, orbitMatrix);
            matrixCalculations++;
        }
        void incOrbit() {
            orbitIncrements++;
            var tot = orbitIncrements * 0.1;
            if (tot > MathHelperD.TwoPi) {
                calculateOrbitMatrix();
            }
            Vector3D.TransformNormal(ref orbit, ref orbitMatrix, out orbit);
        }

        Vector3D dirToThing => Vector3D.Normalize(mEntity.Position - ctr.Volume.Center);
        // position that we want to fly at
        Vector3D orbitProj() {
            var dir = dirToThing;
            return MAF.orthoProject(orbitPos, mEntity.WorldVolume.Center + -dir * space, dir);
        }

        
        // space to keep between us and thing
        double space => ctr.Volume.Radius + PADDING + mEntity.WorldVolume.Radius;

        // position on the far side of the thing
        Vector3D orbitPos =>
            mEntity.WorldVolume.Center + orbit * space;
        public override void Update() {
            ctr.Thrust.Damp = false;
 
            var dest = orbitProj();
            var disp = dest - ctr.Volume.Center;
            var dist = disp.LengthSquared();
            if (mDistToDest < 100) {
                ctr.logger.log($"orbitIncrements={orbitIncrements}");
                ctr.logger.log($"matrixCalculations={matrixCalculations}");
                var dir = disp;
                var mag = dir.Normalize();
                dir = MAF.world2dir(dir, ctr.MyMatrix);
                var desiredVelo = dir * 5.0;
                ctr.Thrust.Acceleration = (desiredVelo - ctr.LocalLinearVelo);
                ctr.Gyro.SetTargetDirection(ctr.LinearVelocityDirection);
                ctr.Gyro.SetRollTarget(mEntity.Position);
                onScan();
                disp = orbitPos - ctr.Volume.Center;
                dist = disp.LengthSquared();
                if (dist < 10000) {
                    incOrbit();
                }
            } else {
                ctr.logger.persist("Out of orbit");
                base.Update();
                collisionDetectTo();
            }
            //ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            var ops = new OPS(Volume.Center, Volume.Radius, ctr.Grid.WorldVolume.Center);
            //ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            //ctr.logger.log(ops);
            
        }
        Action onScan;
        void updateScan() {
            if (mEntity.mOres.Count < updateIndex) {
                var ore = mEntity.mOres[updateIndex];
                if (oreScan(mEntity, ore.Position) < 2) {
                    mEntity.mOres.RemoveAtFast(updateIndex);
                    ctr.logger.persist("Ore removed.");
                }
                updateIndex++;
            } else {
                updateIndex = 0;
            }
        }
        void analyzeScan() {
            var wv = ctr.Volume;
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
            ctr.Camera.Scan(scanPos, out entity, out thy);
            if (thy != null && (thy.Type == ThyDetectedEntityType.Asteroid || thy.Type == ThyDetectedEntityType.AsteroidCluster)) {
                oreScan(thy, scanPos);
            }
            scanPos = wv.Center + ctr.LinearVelocityDirection * wv.Radius * 2.0;
            scanPos += MAF.ranDir() * wv.Radius * 2.0;
            ctr.Camera.Scan(scanPos, out entity, out thy);
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thy"></param>
        /// <param name="aPos"></param>
        /// <returns>0 = scan fail, 1 = scan empty, 2 = scan found ore, 3 ore is new</returns>
        int oreScan(ThyDetectedEntityInfo thy, Vector3D aPos) {
            int result = 0;
            foreach (var detector in mDetectors) {
                var range = detector.GetValue<double>("AvailableScanRange");
                range *= range;
                var disp = detector.WorldMatrix.Translation - aPos;
                var dist = disp.LengthSquared();
                dist += 25;
                if (dist > range) {
                    continue;
                }
                
                detector.SetValue("RaycastTarget", aPos);
                var res = detector.GetValue<MyDetectedEntityInfo>("RaycastResult");
                if (res.TimeStamp != 0) {
                    result = 1;
                    if (res.Name != "") {
                        result = 2;
                        if (thy.AddOre(res)) {
                            result = 3;
                            ctr.logger.persist($"New {res.Name} Deposit found!");
                        }
                    }
                    break;
                } else {
                    ctr.logger.persist("ORE Timestamp Zero");
                }
                //detector.SetValue("ScanEpoch", 0L);
                //throw new Exception("shouldnt be able to write ScanEpoch");
            }
            if (result == 0) {
                ctr.logger.persist("Ore Scan Failure");
            }
            return result;
        }
    }
}
