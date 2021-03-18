using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {

    class CameraList : BlockDirList<IMyCameraBlock> {
        readonly IMyCubeGrid Grid;
        public CameraList(IMyCubeGrid aGrid) {
            Grid = aGrid;
        }

        public bool Scan(Vector3D aTargetWorld, out MyDetectedEntityInfo aEntity, double aAddDist = 0) {
            var targetLocal = MAF.world2pos(aTargetWorld, Grid.WorldMatrix);
            var targetNormal = Vector3D.Normalize(targetLocal);
            //ModuleManager.logger.persist(targetNormal);
            //ModuleManager.logger.persist("targetNormal");


            foreach (var c in pickList(targetNormal)) {
                var dir = aTargetWorld - c.WorldMatrix.Translation;
                var dist = dir.Normalize() + aAddDist;
                //ModuleManager.logger.log(c.CustomName);
                if (c.AvailableScanRange > dist) {
                    double azimuth, elevation;
                    if (testCameraAngles(c, dir)) {
                        Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
                        azimuth = -(azimuth * (180.0 / Math.PI));
                        elevation = (elevation * (180.0 / Math.PI));
                        aEntity = c.Raycast(dist, (float)elevation, (float)azimuth);

                        if (aEntity.Type != MyDetectedEntityType.None) {
                            if (aEntity.EntityId == ModuleManager.Program.Me.CubeGrid.EntityId) {
                                continue;
                            }
                            if (ModuleManager.ConnectedGrid(aEntity.EntityId)) {
                                continue;
                            }
                            return true;
                        }
                    }
                }
            }
            aEntity = new MyDetectedEntityInfo();
            return false;
        }
        bool testCameraAngles(IMyCameraBlock camera, Vector3D aDirection) {
            aDirection = Vector3D.Rotate(aDirection, MatrixD.Transpose(camera.WorldMatrix));

            if (aDirection.Z > 0) //pointing backwards
                return false;

            var yawTan = Math.Abs(aDirection.X / aDirection.Z);
            var pitchTanSq = aDirection.Y * aDirection.Y / (aDirection.X * aDirection.X + aDirection.Z * aDirection.Z);

            return yawTan <= 1 && pitchTanSq <= 1;

        }
        List<IMyCameraBlock> pickList(Vector3D targetLocal) {
            var cd = Base6Directions.GetClosestDirection(targetLocal);
            switch (cd) {
                case Base6Directions.Direction.Forward: return mFront;
                case Base6Directions.Direction.Backward: return mBack;
                case Base6Directions.Direction.Left: return mLeft;
                case Base6Directions.Direction.Right: return mRight;
                case Base6Directions.Direction.Up: return mUp;
            }
            return mDown;
        }
    }

}
