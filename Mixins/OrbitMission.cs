using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    public  class OrbitMission : MissionBase {
        int orbit = -1;
        bool orbitStarted;
        bool orbitLocked;
        // this orbit isn't great 
        // this should be updated to begin an arbitrary orbit in any direction based on the approach
        // I just wanted to get something going quickly and move on to drill docking
        public OrbitMission(ShipControllerModule aController, ThyDetectedEntityInfo aEntity) : base(aController, aEntity) {
            findStart(mEntity.Orientation);
        }

        void findStart(MatrixD o) {
            var up = orbitPos(o.Up) - ctr.Grid.WorldVolume.Center;
            var right = orbitPos(o.Right) - ctr.Grid.WorldVolume.Center;
            if (up.LengthSquared() > right.LengthSquared()) {
                orbit = 2;
            } else {
                orbit = 1;
            }
        }
        Vector3D orbitPos(Vector3D dir) {
            return mEntity.WorldVolume.Center + dir * Altitude;
        }
        public override void Update() {
            base.Update();

            if (mDistToDest < 10 || orbitStarted) {
                orbitStarted = true;
                
                //ctr.logger.log("Orbiting ", orbit);
                Vector3D dir;
                switch (orbit) {
                    case 0:
                        dir = mEntity.Orientation.Up;
                        break;
                    case 1:
                        dir = mEntity.Orientation.Right;
                        break;
                    case 2:
                        dir = mEntity.Orientation.Down;
                        break;
                    default:
                        dir = mEntity.Orientation.Left;
                        break;
                }
                var pos = orbitPos(dir);
                var proj = MAF.orthoProject(pos, Target, mDirToDest);
                dir = proj - ctr.Grid.WorldVolume.Center;
                var dist = dir.Normalize();
                if (orbitLocked) {
                    //ctr.logger.log("Orbit is stable, distance to orbit state change ", dist - 10);
                } else {
                    //ctr.logger.log("Orbit is unstable, distance to stable orbit ", dist - 10);
                }
                if (dist < 10) {
                    orbitLocked = true;
                    orbit++;
                    if (orbit == 4) {
                        orbit = 0;
                    }
                }
                dir = MAF.world2dir(dir, ctr.MyMatrix);
                var desiredVelo = dir * 15.0;
                ctr.Thrust.Acceleration = (desiredVelo - ctr.LocalLinearVelo);
                ctr.Gyro.SetTargetDirection(ctr.LinearVelocityDirection);
            } else {
                //ctr.logger.log("Approaching");
                collisionDetectTo();
            }
            //ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            var ops = new OPS(Volume.Center, Volume.Radius, ctr.Grid.WorldVolume.Center);
            //ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            //ctr.logger.log(ops);
            
        }
    }
}
