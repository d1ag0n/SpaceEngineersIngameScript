using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    public  class OrbitMission : MissionBase {
        public OrbitMission(ShipControllerModule aController, ThyDetectedEntityInfo aEntity) : base(aController, aEntity) {
            
        }

        public override void Update() {
            base.Update();
            if (mDistToDest < 100) {

            } else {
            
            }
            ctr.logger.log("Orbit Mission Distance ", mDistToDest);
            var ops = new OPS(Volume.Center, Volume.Radius, ctr.Grid.WorldVolume.Center);
            ctr.logger.log("Grid Radius: ", ctr.Grid.WorldVolume.Radius);
            ctr.logger.log(ops);
            collisionDetectTo();
        }
    }
}
