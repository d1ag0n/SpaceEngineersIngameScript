using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    public  class OrbitMission : MissionBase {
        int orbit = -1;
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

            if (mDistToDest < 100) {
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
                if (dist < 100) {
                    orbit++;
                    if (orbit == 4) {
                        orbit = 0;
                    }
                }
                var desiredVelo = dir * 10.0;
                var veloCorrection = desiredVelo - ctr.ShipVelocities.LinearVelocity;
            } else {
                collisionDetectTo();
            }
            ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            var ops = new OPS(Volume.Center, Volume.Radius, ctr.Grid.WorldVolume.Center);
            ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            ctr.logger.log(ops);
            
        }
    }
}
