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

        public Mission(ModuleManager aManager, ThyDetectedEntityInfo aDestination) :  base(aManager) { }
            

        public Mission(ModuleManager aManager, Vector3D aPos) : base(aManager) { }

        public override void Update() {
            base.Update();
            if (mDistToDest < mController.Volume.Radius) {
                Complete = true;
            } else {
                collisionDetectTo();
            }
        }




        
    }
}
