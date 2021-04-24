using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript
{
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class Mission : APMission {
        Vector3D mStart;
        Vector3D mDest;
        protected readonly GyroModule mGyro;
        public Mission(ModuleManager aManager, ThyDetectedEntityInfo aEntity) :  base(aManager, aEntity) { }
            

        public Mission(ModuleManager aManager, Vector3D aPos) : base(aManager, null) {
            aManager.GetModule(out mGyro);
            mStart = mController.Volume.Center;
            mDest = aPos;
            mDestination = new BoundingSphereD(aPos, 1d);
            var disp = aPos - mController.Volume.Center;
            var dir = disp;
            dir.Normalize();
            mGyro.Active = true;
            mThrust.Active = true;
            mThrust.Damp = false;
            mGyro.SetTargetDirection(dir);
            //mGyro.SetTargetDirection(Vector3D.Zero);
        }

        public override void Update() {
            base.Update();
            mLog.log(mLog.gps("mDestination", mDestination.Center));
            mLog.log(mLog.gps("mStart", mStart));
            mLog.log(mLog.gps("mDest", mDest));
            if (mDistToDest < mController.Volume.Radius) {
                mThrust.Damp = true;
            } else {
                //FlyTo();
                collisionDetectTo();
            }
        }




        
    }
}
