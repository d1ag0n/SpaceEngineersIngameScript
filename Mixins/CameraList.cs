using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {

    class CameraList {//: BlockDirList<IMyCameraBlock> {
        static int count = 0;
        public readonly List<IMyCameraBlock> mList = new List<IMyCameraBlock>();
        //readonly IMyCubeGrid Grid;
        public CameraList() {}
        public bool Scan(Vector3D aTargetWorld, out MyDetectedEntityInfo aEntity, double aAddDist = 0) {
            ModuleManager.Program.Me.CustomData = aTargetWorld.ToString() + Environment.NewLine;
            var targetLocal = MAF.world2pos(aTargetWorld, ModuleManager.WorldMatrix);
            var targetNormal = Vector3D.Normalize(targetLocal);
            int time = int.MaxValue;
            
            //sb.AppendLine(list.Count + " cameras in list");
            count++;
            //sb.AppendLine(ModuleManager.logger.gps("aTargetWorld " + count, aTargetWorld));
            foreach (var c in mList) {
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
                            if (aEntity.EntityId == ModuleManager.Program.Me.CubeGrid.EntityId) {
                                //sb.AppendLine(c.CustomName + " scanned own grid " + aEntity.EntityId + " " + azimuth + " " + elevation);
                                //sb.AppendLine(ModuleManager.logger.gps(c.CustomName, c.WorldMatrix.Translation));
                                continue;
                            }
                            if (ModuleManager.ConnectedGrid(aEntity.EntityId)) {
                                //sb.AppendLine("Connected grid");
                                continue;
                            }
                            //sb.AppendLine(ModuleManager.logger.gps(aEntity.Type + " " + aEntity.Name, aEntity.HitPosition.Value));
                        }
           
                        return true;
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
