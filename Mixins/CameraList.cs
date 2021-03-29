using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {

    class CameraList : BlockDirList<IMyCameraBlock> {
        //public readonly List<IMyCameraBlock> mList = new List<IMyCameraBlock>();
        //readonly IMyCubeGrid Grid;
        readonly CameraModule mCamera;
        public CameraList(CameraModule aMod) {
            mCamera = aMod;
        }
        Base6Directions.Direction GetClosestDirection(Vector3D v) {
            Base6Directions.Direction result = Base6Directions.Direction.Left;

            var x = Math.Abs(v.X);
            var y = Math.Abs(v.Y);
            var z = Math.Abs(v.Z);
            if (v.X > 0) {
                result = Base6Directions.Direction.Right;
            }
            if (y > x) {
                if (v.Y > 0) {
                    result = Base6Directions.Direction.Up;
                } else {
                    result = Base6Directions.Direction.Down;
                }
            }
            if (z > y || z > x) {
                if (v.Z > 0) {
                    result = Base6Directions.Direction.Backward;
                } else {
                    result = Base6Directions.Direction.Forward;
                }
            }
            return result;
        }
        public bool Scan(Vector3D aTargetWorld, out MyDetectedEntityInfo aEntity, double aAddDist = 0) {
            //ModuleManager.Program.Me.CustomData = aTargetWorld.ToString() + Environment.NewLine;
            var targetLocal = MAF.world2pos(aTargetWorld, mCamera.MyMatrix);
            var targetNormal = Vector3D.Normalize(targetLocal);
            var cd = Base6Directions.GetClosestDirection(targetNormal);
            //ModuleManager.logger.persist("vanilla " + cd);
            //cd = GetClosestDirection(targetNormal);
            //ModuleManager.logger.persist("custom " + cd);
            //ModuleManager.logger.persist(ModuleManager.logger.string4(targetNormal));
            int icd = (int)cd;
            var list = mLists[(int)icd];
            //ModuleManager.logger.log("Camera picked " + cd + " " + list.Count + " cameras in list");
            
            //ModuleManager.logger.log("Camera picked " + cd);
            foreach (var c in list) {
                Vector3D dir = aTargetWorld - c.WorldMatrix.Translation;
                var dist = dir.Normalize() + aAddDist;
                //ModuleManager.logger.log(c.CustomName);
                if (c.AvailableScanRange > dist) {
                    double azimuth, elevation;
                    if (testCameraAngles(c, ref dir)) {
                        Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
                        azimuth = -(azimuth * (180.0 / Math.PI));
                        elevation = (elevation * (180.0 / Math.PI));
                        aEntity = c.Raycast(dist, (float)elevation, (float)azimuth);

                        if (aEntity.Type != MyDetectedEntityType.None) {
                            if (aEntity.EntityId == mCamera.mManager.mProgram.Me.CubeGrid.EntityId) {
                                //sb.AppendLine(c.CustomName + " scanned own grid " + aEntity.EntityId + " " + azimuth + " " + elevation);
                                //sb.AppendLine(ModuleManager.logger.gps(c.CustomName, c.WorldMatrix.Translation));
                                continue;
                            }
                            if (mCamera.mManager.ConnectedGrid(aEntity.EntityId)) {
                                //sb.AppendLine("Connected grid");
                                continue;
                            }
                            //sb.AppendLine(ModuleManager.logger.gps(aEntity.Type + " " + aEntity.Name, aEntity.HitPosition.Value));
                        }
           
                        return true;
                    } else {
                        
                        Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
                        azimuth = -(azimuth * (180.0 / Math.PI));
                        elevation = (elevation * (180.0 / Math.PI));
                        for (int i = 0; i < 6; i++) {
                            if (i != icd) {
                                var lst = mLists[i];
                                foreach (var cc in lst) {
                                    dir = aTargetWorld - c.WorldMatrix.Translation;
                                    dir.Normalize();
                                    if (testCameraAngles(cc, ref dir)) {
                                        //ModuleManager.logger.persist("Camera from list " + (Base6Directions.Direction)i + " okay, list " + (Base6Directions.Direction)icd + " bad");
                                        //aEntity = new MyDetectedEntityInfo();
                                        //return false;
                                        //ModuleManager.logger.persist("Camera out of angle: " + azimuth.ToString("f0") + " " + elevation.ToString("f0"));
                                    }
                                }
                                
                            }
                        }
                        
                    }
                }
            }
            aEntity = new MyDetectedEntityInfo();

            return false;
        }
        bool testCameraAngles(IMyCameraBlock camera, ref Vector3D aDirection) {
            aDirection = Vector3D.Rotate(aDirection, MatrixD.Transpose(camera.WorldMatrix));
            if (aDirection.Z > 0)
                return false;

            var yawTan = Math.Abs(aDirection.X / aDirection.Z);
            var pitchTanSq = aDirection.Y * aDirection.Y / (aDirection.X * aDirection.X + aDirection.Z * aDirection.Z);

            return yawTan <= 1 && pitchTanSq <= 1;

        }
     
    }

}
