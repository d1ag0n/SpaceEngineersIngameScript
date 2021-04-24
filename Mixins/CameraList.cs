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
        bool scanWith(IMyCameraBlock aCamera, ref Vector3D aWorldPosition, ref MyDetectedEntityInfo aEntity, double aAddDistance, out bool aRangeLow) {
            if (!aCamera.EnableRaycast) {
                throw new Exception("Raycast disabled.");
            }
            var worldDisplacement = aWorldPosition - aCamera.WorldMatrix.Translation;
            var worldNormal = worldDisplacement;
            var dist = worldNormal.Normalize();
            var result = aCamera.CanScan(dist + aAddDistance, worldNormal);
            aRangeLow = false;
            if (result) {
                aEntity = aCamera.Raycast(dist + aAddDistance, MAF.world2dir(worldNormal, aCamera.WorldMatrix));
                if (aEntity.HitPosition.HasValue) {
                    if (aEntity.Type != MyDetectedEntityType.None && (aEntity.EntityId == mCamera.Grid.EntityId || mCamera.mManager.ConnectedGrid(aEntity.EntityId))) {
                        result = false;
                    }
                }
                
            } else {
                if (aCamera.AvailableScanRange < dist + aAddDistance) {
                    aRangeLow = true;
                }
            }
            return result;
        }
        // whiplash141 - https://discord.com/channels/125011928711036928/216219467959500800/810367147489361960
        bool testCameraAngles(IMyCameraBlock camera, ref Vector3D aWorldNormal, out Vector3D aLocalNormal) {
            var m = MatrixD.Transpose(camera.WorldMatrix);
            Vector3D.Rotate(ref aWorldNormal, ref m, out aLocalNormal);
            if (aLocalNormal.Z < 0) {
                if (Math.Abs(aLocalNormal.X / aLocalNormal.Z) <= 1) {
                    return aLocalNormal.Y * aLocalNormal.Y / (aLocalNormal.X * aLocalNormal.X + aLocalNormal.Z * aLocalNormal.Z) <= 1;
                }
            }
            return false;
        }
        public bool Scan(ref Vector3D aWorldPosition, ref MyDetectedEntityInfo aEntity, double aAddDist = 0) {
            if (!aWorldPosition.IsValid()) {
                mCamera.mLog.persist("CameraList.Scan invalid aTargetWorld.");
                aEntity = default(MyDetectedEntityInfo);
                return false;
            }

            var targetNormal = Vector3D.Normalize(MAF.world2pos(aWorldPosition, mCamera.MyMatrix));
            
            if (!targetNormal.IsValid()) {
                mCamera.mLog.persist("CameraList.Scan invalid targetNormal.");
                aEntity = default(MyDetectedEntityInfo);
                return false;
            }
            var cd = Base6Directions.GetClosestDirection(targetNormal);
            var list = mLists[(int)cd];
            var lowRange = false;
            bool rangeLow;
            foreach (var c in mCamera.Blocks) {
                if (scanWith(c, ref aWorldPosition, ref aEntity, aAddDist, out rangeLow)) {
                    return true;
                } else {
                    if (rangeLow) {
                        lowRange = true;
                    }
                }
            }
            if (lowRange) {
                mCamera.mLog.persist("Camera range low.");
            }
            aEntity = new MyDetectedEntityInfo();
            return false;
        }

     
    }

}
