using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {
    class CDMission : MissionBase {
        readonly ThrustModule mThrust;
        readonly GyroModule mGyro;
        readonly CameraModule mCamera;
        Vector3D mDestination;
        readonly BoundingBoxD mStart;
        public CDMission(ModuleManager aManager, Vector3D aDestination) : base(aManager) {
            aManager.GetModule(out mThrust);
            mGyro.GetModule(out mGyro);
            mCamera.GetModule(out mCamera);
            mDestination = aDestination;
            mStart = BOX.GetCBox(mController.Remote.CenterOfMass);
        }
        public override void Update() {
            var obb = MAF.obb(mController.Grid, 0.5);
        }

    }
}
