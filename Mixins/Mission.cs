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
        

        public Mission(ShipControllerModule aController, ThyDetectedEntityInfo aDestination) : base(aController, aDestination) { }
        
        public override void Update() {
            base.Update();
            collisionDetectTo();
            
            

        }




        
    }
}
