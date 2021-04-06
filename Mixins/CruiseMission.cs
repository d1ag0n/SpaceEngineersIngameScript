using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    public class CruiseMission : MissionBase {
        double lastElevation;
        double Altitude;
        const double pitch = -0.0001;
        const double maxDif = 5.0;
        readonly ShipControllerModule mController;
        readonly ThrustModule mThrust;
        readonly GyroModule mGyro;
        readonly LogModule mLog;
        bool isActive = false;
        bool doThrust = false;
        double Velocity;


        public CruiseMission(ModuleManager aManager):base(aManager) {
            aManager.GetModule(out mController);
            aManager.GetModule(out mThrust);
            aManager.GetModule(out mGyro);
            aManager.GetModule(out mLog);
            
            mThrust.Damp = false;
            mThrust.Emergency = true;
            mThrust.Active = true;
            mGyro.Active = true;
        }
        public override bool Cancel() => true;
        public override void Input(string arg) {
            arg = arg.ToLower().Trim();
            if (arg == "off") {
                isActive = mThrust.Active = mGyro.Active = false;
                mThrust.InitAction();
            } else if (arg.StartsWith("thrust")) {
                mController.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out Altitude);
                isActive = doThrust = mThrust.Active = mGyro.Active = true;
                if (arg.Length > 6) {
                    if (double.TryParse(arg.Substring(6).Trim(), out Velocity)) {
                        Velocity = Math.Abs(Velocity);
                    } else {
                        Velocity = mController.LinearVelocity;
                    }
                } else {
                    Velocity = mController.LinearVelocity;
                }
            } else if (arg == "nothrust") {
                mThrust.InitAction();
                mController.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out Altitude);
                isActive = mGyro.Active = true;
                doThrust = mThrust.Active = false;
            }
        }
        public override void Update() {
            if (!isActive)
                return;
            //mLog.log($"Mission Altitude {Altitude:f0}");
            var dir = MAF.world2dir(mController.Remote.WorldMatrix.Forward, mController.MyMatrix);
            var grav = MAF.world2dir(mController.Remote.GetNaturalGravity(), mController.MyMatrix);
            var axis = dir.Cross(grav);
            //var ab = MAF.angleBetween(dir, grav);

            double elevation;
            mController.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out elevation);
            //mController.logger.log($"Current Altitude {elevation:f0}");

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
                mGyro.SetTargetDirection(MAF.local2dir(dir, mController.MyMatrix));
            }
            lastElevation = elevation;
            if (doThrust) {
                //mLog.log($"Velocity {Velocity}");
                var vec = dir * Velocity;
                var accel = vec - mController.LocalLinearVelo;
                mThrust.Acceleration = accel;
            }
        }
    }
}
