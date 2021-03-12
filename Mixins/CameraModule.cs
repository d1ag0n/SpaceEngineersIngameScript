using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    class CameraModule : Module<IMyCameraBlock>
    {
        readonly List<MyDetectedEntityInfo> mDetected = new List<MyDetectedEntityInfo>();
        public bool hasCamera => Blocks.Count > 0;

        public CameraModule() {
            MenuName = "Camera Records";
            Save = SaveDel;
            Load = LoadDel;
        }

        void SaveDel(Serialize s) {
            logger.persist("CameraModule.SaveDel");
            var one = false;
            foreach (var e in mDetected) {
                if (one) {
                    s.rec();
                }
                s.unt("Record");
                s.str(e);
                one = true;
            }

        }

        void LoadDel(Serialize s, string aData) {
            var ar = aData.Split(Serialize.RECSEP);
            foreach (var record in ar) {
                var entry = record.Split(Serialize.UNTSEP);
                if (entry[0] == "Record") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        mDetected.Add(s.objMyDetectedEntityInfo(new Stringerator(entries)));
                    }
                }
            }
        }

        public override List<object> MenuMethods(int aPage) {
            var index = aPage * 6;
            var result = new List<object>();
            //logger.persist($"CameraModule.MenuMethods({aPage});");
            //logger.persist($"index={index}");
            //logger.persist($"mDetected.Count={mDetected.Count}");
            for (int i = index; i < index + 6; i++) {
                if (mDetected.Count > i) {
                    var e = mDetected[i];
                    result.Add(new MenuMethod($"{e.Name} {e.EntityId}", e, EntityMenu));
                } else {
                    break;
                }
            }
            return result;
        }



        public void Add(MyDetectedEntityInfo aEntity) => mDetected.Add(aEntity);

        Menu EntityMenu(MenuModule aMain, object aState) {
            
            var list = new List<object>();
            var e = (MyDetectedEntityInfo)aState;
            
            list.Add($"Time: {e.TimeStamp}"); 
            list.Add($"Relationship: {e.Relationship}");
            
            if (e.HitPosition.HasValue) {
                list.Add(logger.gps(e.Name + " Hit", e.HitPosition.Value));
            }
            //list.Add(new MenuMethod(logger.gps(e.Name + " Position", e.Position), aState, null));
            list.Add(logger.gps(e.Name + " Position", e.Position));
            
            return new Menu(aMain, $"Camera Record for {e.Name} {e.EntityId}", p => list);
        }
        /*Menu EntityGPS(MenuModule aMain, object aState) {
            var e = (MyDetectedEntityInfo)aState;
            logger.persist(logger.gps(e.Name, e.HitPosition.Value));
            return null;
        }*/
        



        public override bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is IMyMotorStator) {
                if (ModuleManager.HasTag(aBlock, "camera")) {
                    var rotor = aBlock as IMyMotorStator;
                    rotor.Enabled = true;
                    rotor.RotorLock = false;
                    rotor.BrakingTorque = 0;
                    rotor.Torque = 1000.0f;
                    rotor.TargetVelocityRad = 1.0f;
                    
                }
                return false;
            }
            var result = base.Accept(aBlock);
            if (result) {
                var camera = aBlock as IMyCameraBlock;
                camera.Enabled = true;
                camera.EnableRaycast = true;
            }
            return result;
        }
        /// <summary>
        /// returns true if we found a camera that will scan the target location and scanned it
        /// </summary>
        /// <param name="aTarget"></param>
        /// <param name="aAddDistance"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool Scan(Vector3D aTarget, out MyDetectedEntityInfo aEntity, double aAddDist = 0) {

            foreach (var camera in Blocks) { 
                var dir = aTarget - camera.WorldMatrix.Translation;
                var dist = dir.Normalize() + aAddDist;

                double azimuth = 0, elevation = 0;

                if (camera.AvailableScanRange > dist) {
                    /*if (testCameraAngles(c, dir, ref yaw, ref pitch)) {
                        e = c.Raycast(dist, (float)pitch, (float)yaw);
                        return true;
                    }*/

                    if (testCameraAngles(camera, ref dir)) {
                        Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
                        azimuth = -(azimuth * (180.0 / Math.PI));
                        elevation = (elevation * (180.0 / Math.PI));
                        aEntity = camera.Raycast(dist, (float)elevation, (float)azimuth);

                        if (aEntity.Type != MyDetectedEntityType.None) {
                            if (aEntity.EntityId == ModuleManager.Program.Me.CubeGrid.EntityId) {
                                continue;
                            }
                            mDetected.Add(aEntity);
                            return true;
                        }
                    }
                }
            }
            aEntity = default(MyDetectedEntityInfo);
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
        /*
        /// Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        * /
        void GetRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double yaw, out double pitch) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

            yaw = AngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
            if (Math.Abs(yaw) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                yaw = Math.PI;

            if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                pitch = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
            else
                pitch = AngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive
        }//*/
    }
}
