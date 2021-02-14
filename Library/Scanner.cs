using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Scanner
    {
        readonly List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
        
        readonly Logger g;
        readonly MyGridProgram program;

        public bool hasCamera => cameras.Count > 0;

        public Scanner(MyGridProgram aProgram, GTS aGTS, Logger aG) {
            program = aProgram;            
            g = aG;

            aGTS.initList(cameras, false);
            var rotors = new List<IMyMotorStator>();
            aGTS.initListByTag("camera", rotors, false);

            foreach (var r in rotors) {
                r.Enabled = true;
                r.RotorLock = false;
                r.BrakingTorque = 0;
                r.Torque = 1000.0f;
                r.TargetVelocityRad = 1.0f;
            }

            foreach (var c in cameras) {
                c.EnableRaycast = true;
            }
        }
        /// <summary>
        /// returns true if we found a camera that will scan the target location
        /// </summary>
        /// <param name="aTarget"></param>
        /// <param name="aAddDistance"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool Scan(Vector3D aTarget, ref MyDetectedEntityInfo aEntity, double aAddDistance = 0) {

            for (int i = 0; i < cameras.Count; i++) {
                var c = cameras[i];
                var dir = aTarget - c.WorldMatrix.Translation;
                var dist = dir.Normalize() + aAddDistance;

                double azimuth = 0, elevation = 0;
                if (c.AvailableScanRange > dist) {
                    /*if (testCameraAngles(c, dir, ref yaw, ref pitch)) {
                        e = c.Raycast(dist, (float)pitch, (float)yaw);
                        return true;
                    }*/

                    if (testCameraAngles(c, ref dir)) {
                        Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
                        azimuth = -(azimuth * (180.0 / Math.PI));
                        elevation = (elevation * (180.0 / Math.PI));
                        var e = c.Raycast(dist, (float)elevation, (float)azimuth);
                        
                        if (e.EntityId != program.Me.CubeGrid.EntityId) {
                            aEntity = e;
                        } else {
                            if (i < cameras.Count - 1) {
                                continue;
                            }
                        }
                        return true;
                    }
                    
                }
            }
            return false;
        }

        /*
         TransformNormal == Rotate
         Exact same code
        */
        /// <summary>
        /// whiplash
        /// https://discord.com/channels/125011928711036928/216219467959500800/810367147489361960
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        bool testCameraAnglesAE(IMyCameraBlock aCamera, Vector3D aDirection, ref double aYawTan, ref double aPitchTan1Sq, ref double aPitchTan2Sq) {
            aDirection = Vector3D.Rotate(aDirection, MatrixD.Transpose(aCamera.WorldMatrix));

            if (aDirection.Z > 0) {
                return false;
            }
            aYawTan = aDirection.X / aDirection.Z;
            aPitchTan1Sq = aDirection.X * aDirection.X;
            aPitchTan2Sq = aDirection.Z * aDirection.Z;
            var pitchTanSq = aDirection.Y * aDirection.Y / (aPitchTan1Sq + aPitchTan2Sq);

            return Math.Abs(aYawTan) <= 1 && pitchTanSq <= 1;
        }
        bool testCameraAngles(IMyCameraBlock camera, ref Vector3D aDirection) {
            aDirection = Vector3D.Rotate(aDirection, MatrixD.Transpose(camera.WorldMatrix));

            if (aDirection.Z > 0) //pointing backwards
                return false;

            var yawTan = Math.Abs(aDirection.X / aDirection.Z);
            var pitchTanSq = aDirection.Y * aDirection.Y / (aDirection.X * aDirection.X + aDirection.Z * aDirection.Z);

            return yawTan <= 1 && pitchTanSq <= 1;
        }
        bool zTestCameraAngles(IMyCameraBlock camera, Vector3D direction) {
            Vector3D localDirection = Vector3D.Rotate(direction, MatrixD.Transpose(camera.WorldMatrix));

            if (localDirection.Z > 0) //pointing backwards
                return false;

            var yawTan = Math.Abs(localDirection.X / localDirection.Z);
            var pitchTan = Math.Abs(localDirection.Y / localDirection.Z);

            return yawTan <= 1 && pitchTan <= 1;
        }
    }
}
