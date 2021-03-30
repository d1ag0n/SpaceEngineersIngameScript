using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    public  class OrbitMission : MissionBase {
        Vector3D orbit;
        Vector3D orbitAxis;
        MatrixD orbitMatrix;
        int orbitIncrements = 0;
        
        // this orbit is okayish I hope testing now
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking
        public OrbitMission(ShipControllerModule aController, ThyDetectedEntityInfo aEntity) : base(aController, aEntity) {
            orbit = Vector3D.Normalize(mEntity.Position - ctr.Volume.Center);
            orbit.CalculatePerpendicularVector(out orbitAxis);
            MatrixD.CreateFromAxisAngle(ref orbitAxis, 0.1, out orbitMatrix);
        }

        Vector3D dirToThing => Vector3D.Normalize(mEntity.Position - ctr.Volume.Center);
        // position that we want to fly at
        Vector3D orbitProj() {
            var dir = dirToThing;
            return MAF.orthoProject(orbitPos, mEntity.WorldVolume.Center + -dir * space, dir);
        }
        void incOrbit() {
            orbitIncrements++;
            Vector3D.TransformNormal(ref orbit, ref orbitMatrix, out orbit);
        }
        
        // space to keep between us and thing
        double space => ctr.Volume.Radius + PADDING + mEntity.WorldVolume.Radius;

        // position on the far side of the thing
        Vector3D orbitPos =>
            mEntity.WorldVolume.Center + orbit * space;
        public override void Update() {
            ctr.logger.log($"mEntity.WorldVolume.Radius={mEntity.WorldVolume.Radius}");
            ctr.Damp = false;

 
            var dest = orbitProj();
            var disp = dest - ctr.Volume.Center;
            var dist = disp.LengthSquared();
            ctr.logger.log($"dist={dist}");
            ctr.logger.log($"mDistToDest={mDistToDest}");
            if (mDistToDest < 100) {
                
                
                ctr.logger.log($"orbitIncrements={orbitIncrements}");
                var dir = disp;
                var mag = dir.Normalize();
                ctr.logger.log($"mag={mag}");
                dir = MAF.world2dir(dir, ctr.MyMatrix);
                var desiredVelo = dir * 5.0;
                ctr.Thrust.Acceleration = (desiredVelo - ctr.LocalLinearVelo);
                ctr.Gyro.SetTargetDirection(ctr.LinearVelocityDirection);
                ctr.Gyro.SetRollTarget(mEntity.Position);
                scan();
                disp = orbitPos - ctr.Volume.Center;
                dist = disp.LengthSquared();
                ctr.logger.log($"dist={dist}");
                if (dist < 10000) {
                    incOrbit();
                }
            } else {
                base.Update();
                
                collisionDetectTo();
            }
            //ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            var ops = new OPS(Volume.Center, Volume.Radius, ctr.Grid.WorldVolume.Center);
            //ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            //ctr.logger.log(ops);
            
        }
        void scan() {
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
            scanPos = wv.Center + ctr.LinearVelocityDirection * wv.Radius * 2.0;
            scanPos += MAF.ranDir() * wv.Radius * 2.0;
            ctr.Camera.Scan(scanPos, out entity, out thy);
            
        }
    }
}
