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
        const double pitch = 0.0001;
        const double amp = 10d;
        const double maxDif = 10d;
        const double prefVert = 2d;
        readonly ThrustModule mThrust;
        readonly GyroModule mGyro;
        readonly LogModule mLog;
        bool isActive = false;
        bool doThrust = false;
        double Velocity;
        


        public CruiseMission(ModuleManager aManager):base(aManager) {
            aManager.GetModule(out mThrust);
            aManager.GetModule(out mGyro);
            aManager.GetModule(out mLog);
            
            mThrust.Damp = false;
            mThrust.Emergency = true;
            mThrust.Active = true;
            mGyro.Active = true;
        }
        public override void Input(string arg) {
            arg = arg.ToLower().Trim();
            if (arg == "off") {
                mController.AllDampers(true);
                isActive = mThrust.Active = mGyro.Active = false;
                mThrust.AllStop();
            } else if (arg.StartsWith("thrust")) {
                mController.AllDampers(false);
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
                
                mThrust.AllStop();
                mController.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out Altitude);
                isActive = mGyro.Active = true;
                doThrust = mThrust.Active = false;
            }
        }
        public override void Update() {
            if (!isActive)
                return;
            var worldGrav = mController.Remote.GetNaturalGravity();
            var localGrav = MAF.world2dir(worldGrav, mController.MyMatrix);
            var worldAxis = worldGrav.Cross(mController.LinearVelocityDirection);
            //var localAxis = MAF.world2dir(worldAxis, mController.MyMatrix); ;
            double elevation;
            mController.Remote.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out elevation);
            mLog.log($"Mission Altitude={Altitude}");
            mLog.log($"Current Altitude={elevation}");
            var verticalSpeed = elevation - lastElevation;
            
            var absAltDif = Math.Abs(elevation - Altitude);
            mLog.log($"verticalSpeed={verticalSpeed}");
            mLog.log($"absAltDif={absAltDif}");
            var angle = 0d;
            if (absAltDif < maxDif) {
                if (elevation > Altitude && verticalSpeed > -0.01) {
                    angle = -pitch;
                } else if (elevation < Altitude && verticalSpeed < 0.01) {
                    angle = pitch;
                }
            } else if (elevation < Altitude) {
                if (verticalSpeed < 0d) {
                    angle = pitch * amp;
                } else if (verticalSpeed > prefVert) {
                    angle = -pitch;
                } else {
                    angle = pitch;
                }
            } else if (elevation > Altitude) {
                if (verticalSpeed > 0) {
                    angle = -pitch * amp;
                } else if (verticalSpeed < -prefVert) {
                    angle = pitch;
                } else {
                    angle = -pitch;
                }
            }
            
            mLog.log($"angle={angle}");
            
            if (angle != 0d) {
                var rot = MatrixD.CreateFromAxisAngle(worldAxis, angle);
                var dir = Vector3D.Rotate(mController.Remote.WorldMatrix.Forward, rot);
                mGyro.SetTargetDirection(dir);
            }
            lastElevation = elevation;
            if (doThrust) {
                //mLog.log($"Velocity {Velocity}");
                var vec = MAF.world2dir(mController.Remote.WorldMatrix.Forward, mController.MyMatrix) * Velocity;
                var accel = vec - mController.LocalLinearVelo;
                mThrust.Acceleration = (accel * 5) + (-localGrav / 6d);
            }
        }
    }
}
