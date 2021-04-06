using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    public class CruiseMission : MissionBase {
        readonly double Altitude;
        const double pitch = -0.0001;
        const double maxDif = 1.0;
        public CruiseMission(ShipControllerModule aController):base(aController, default(BoundingSphereD)) {
            ctr.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out Altitude);
            ctr.Thrust.Damp = false;
            ctr.Thrust.Emergency = true;
            ctr.Thrust.Active = true;
            ctr.Gyro.Active = true;
        }
        double lastElevation;
        public override void Update() {
            ctr.logger.log($"Mission Altitude {Altitude:f0}");
            var dir = MAF.world2dir(ctr.Remote.WorldMatrix.Forward, ctr.MyMatrix);
            var grav = MAF.world2dir(ctr.Remote.GetNaturalGravity(), ctr.MyMatrix);
            var axis = dir.Cross(grav);
            //var ab = MAF.angleBetween(dir, grav);

            double elevation;
            ctr.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out elevation);
            ctr.logger.log($"Current Altitude {elevation:f0}");

            var altDif = elevation - lastElevation;

            var absAltDif = Math.Abs(elevation - Altitude);

            var angle = 0d;
            if (absAltDif < maxDif) {
                if (altDif > 0) {
                    angle = -pitch;
                } else {
                    angle = pitch;
                }
            } else if (elevation < Altitude) {
                if (altDif < 0d) {
                    angle = pitch * 5d;
                } else if (altDif > 1d) {
                    angle = -pitch * 5d;
                }
            } else if (elevation > Altitude) {
                if (altDif > 0) {
                    angle = -pitch * 5d;
                } else if (altDif < -1d) {
                    angle = pitch * 5d;
                }
            }
            if (Math.Abs(altDif * 6d) > 1.0d) {
                angle *= 10d;
            }
            if (angle != 0d) {
                var rot = MatrixD.CreateFromAxisAngle(axis, angle);

                dir = Vector3D.Rotate(dir, rot);

                ctr.Gyro.SetTargetDirection(MAF.local2dir(dir, ctr.MyMatrix));
            }

            ctr.logger.log($"altDif={altDif * 6d}");



            lastElevation = elevation;
            
            ctr.logger.log($"thrust active={ctr.Thrust.Active}");

            
            var vec = dir * 50d;
            ctr.logger.log("vec", vec);
            var accel = vec - ctr.LocalLinearVelo;
            ctr.logger.log("accel", accel);
            ctr.Thrust.Acceleration = accel;
        }
    }
}
