using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript
{
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class Mission : MissionBase {

        public Mission(ShipControllerModule aController, ThyDetectedEntityInfo aDestination) : 
            base(aController, aDestination) { }

        public Mission(ShipControllerModule aController, Vector3D aPos) :
            base(aController, new BoundingSphereD(aPos, 0)) { }
        
        public override void Update() {
            base.Update();
            if (mDistToDest < ctr.Volume.Radius) {
                Complete = true;
            } else {
                collisionDetectTo();
            }
        }




        
    }
}
