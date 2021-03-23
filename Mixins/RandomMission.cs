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
        
        public RandomMission(ShipControllerModule aController) : base(aController) {
            
            mDestination = ctr.Remote.CenterOfMass + MAF.ranDir() * 1100.0;
            ctr.logger.persist(ctr.logger.gps("RandomMission", mDestination));
        }
        
        public override void Update() => FlyTo();
    }
}

