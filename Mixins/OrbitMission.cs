using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public  class OrbitMission : APMission {
        protected readonly GyroModule mGyro;
        readonly Vector3D mOriginalOrbit;
        readonly Vector3D mOriginalPerp;
        readonly OreDetectorModule mOre;
        MatrixD mRotOrbit;
        Vector3D mOrbit;
        Vector3D mPerp;
        int orbitIncrements;
        int incrementsSince;
        Action onScan;
        int mMatrixCalculations;

        public OrbitMission(ModuleManager aManager, ThyDetectedEntityInfo aEntity) : base(aManager, aEntity) {
            aManager.GetModule(out mGyro);
            aManager.GetModule(out mOre);
            mOrbit = mOriginalOrbit = Vector3D.Normalize(mController.Volume.Center - mEntity.Position);
            mOriginalOrbit.CalculatePerpendicularVector(out mOriginalPerp);
            mPerp = mOriginalPerp;

            calculateOrbitMatrix();
            onScan = analyzeScan;
            
            
            
        }

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

        // this orbit is okayish
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking

        
        void incOrbit() {
            if (onScan == analyzeScan) {
                incrementsSince++;
                if (incrementsSince > 15) {
                    incrementsSince = 0;
                    OreDetectorModule.UpdateScan(mManager, mEntity);
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

            mLog.log($"orbitIncrements={orbitIncrements}");
            mLog.log($"matrixCalculations={mMatrixCalculations}");
            if (dist < 250000) {
                
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
                base.Update();
                mLog.log(mLog.gps("mDestination", mDestination.Center));
                mLog.log($"Out of orbit: {mDistToDest}");
                collisionDetectTo();
                mGyro.SetTargetDirection(mDirToDest);
            }
            //ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            //var ops = new OPS(Volume.Center, Volume.Radius, mController.Grid.WorldVolume.Center);
            //ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            //ctr.logger.log(ops);
            
        }
        
        
        void analyzeScan() {
            mLog.log($"analyzeScan - ore count {mEntity.mOres.Count}");
            var wv = mController.Volume;
            var dir = MAF.ranDir() * (mEntity.WorldVolume.Radius * 0.9);
            var scanPos = mEntity.Position + dir;

            var dispToShip = wv.Center - mEntity.Position;

            var dispToPos = scanPos - mEntity.Position;

            if (dispToPos.Dot(dispToShip) > 0) {
                scanPos = mEntity.Position + -dir;
            }

            var entity = new MyDetectedEntityInfo();
            ThyDetectedEntityInfo thy;
            mCamera.Scan(ref scanPos, ref entity, out thy);
            if (thy != null && (thy.Type == ThyDetectedEntityType.Asteroid || thy.Type == ThyDetectedEntityType.AsteroidCluster)) {
                MyDetectedEntityInfo info;
                mOre.Scan(thy, scanPos, out info, false);
            }
            scanPos = wv.Center + mController.LinearVelocityDirection * wv.Radius * 4d;
            scanPos += MAF.ranDir() * wv.Radius * 2d;
            mCamera.Scan(ref scanPos, ref entity, out thy);
        }
        
    }
}
