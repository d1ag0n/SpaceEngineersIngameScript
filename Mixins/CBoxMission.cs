using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class CBoxMission : MissionBase {

        public CBoxMission(ShipControllerModule aController, ThyDetectedEntityInfo aEntity) : base(aController, aEntity) {

        }

        public override void Update() {
            base.Update();
            var box = BOX.GetCBox(ctr.Volume.Center);
            
        }
    }
}
