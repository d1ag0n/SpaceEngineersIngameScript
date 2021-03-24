using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class RandomMission : MissionBase {
        static int next = 0;
        
        public RandomMission(ShipControllerModule aController, BoundingSphereD aDestination) : base(aController, aDestination) {
            ctr.logger.persist(ctr.logger.gps("RandomMission", mDestination.Center));
        }
        
        public override void Update() => FlyTo();
    }
}

