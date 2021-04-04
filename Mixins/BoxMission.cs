using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    public class BoxMission : MissionBase {
        Vector3D mTarget;
        public BoxMission(ShipControllerModule aController, Vector3D aPos): base(aController, new BoundingSphereD(aPos, 0d)) {
            ctr.Gyro.SetTargetDirection(Vector3D.Zero);
            ctr.Thrust.Damp = true;
            mTarget = aPos;
        }

        public override void Update() {
            var obb = ctr.OBB;

        }



    }
}
