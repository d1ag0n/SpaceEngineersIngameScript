using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class RandomMission : APMission {
        
        
        public RandomMission(ModuleManager aManager) : base(aManager) {
            //ctr.logger.persist(ctr.logger.gps("RandomMission", Volume.Center));
        }

        public override void Update() {
            //base.Update();
            //FlyTo();
        }
    }
}

