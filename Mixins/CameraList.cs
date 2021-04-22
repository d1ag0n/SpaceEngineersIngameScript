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
        bool scanWith(IMyCameraBlock aCamera, ref Vector3D aWorldPosition, ref MyDetectedEntityInfo aEntity) {
            var worldDisplacement = aWorldPosition - aCamera.WorldMatrix.Translation;
            var worldNormal = worldDisplacement;
            var dist = worldNormal.Normalize();
            var result = aCamera.AvailableScanRange > dist;
            if (result) {
                result = testCameraAngles(aCamera, worldNormal);
                if (result) {
                    aEntity = aCamera.Raycast(dist, worldNormal);
                    if (aEntity.Type != MyDetectedEntityType.None) {
                        if (aEntity.EntityId == mCamera.Grid.EntityId || mCamera.mManager.ConnectedGrid(aEntity.EntityId)) {
                            result = false;
                        }
                    }
                }
            }
            return result;
        }
        // whiplash141 - https://discord.com/channels/125011928711036928/216219467959500800/810367147489361960
        bool testCameraAngles(IMyCameraBlock camera, Vector3D aWorldNormal) {
            aWorldNormal = Vector3D.Rotate(aWorldNormal, MatrixD.Transpose(camera.WorldMatrix));
            if (aWorldNormal.Z < 0) {
                if (Math.Abs(aWorldNormal.X / aWorldNormal.Z) <= 1) {
                    return aWorldNormal.Y * aWorldNormal.Y / (aWorldNormal.X * aWorldNormal.X + aWorldNormal.Z * aWorldNormal.Z) <= 1;
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
            foreach (var c in list) {
                if (scanWith(c, ref aWorldPosition, ref aEntity)) {
                    return true;
                }
            }
            aEntity = new MyDetectedEntityInfo();
            return false;
        }

     
    }

}
