using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class RandomMission : MissionBase {
        
        
        public RandomMission(ShipControllerModule aController, BoundingSphereD aDestination) : base(aController, aDestination) {
            ctr.logger.persist(ctr.logger.gps("RandomMission", Volume.Center));
        }

        public override void Update() {
            base.Update();
            //FlyTo();
        }
    }
}

