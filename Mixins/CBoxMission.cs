using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class CBoxMission : MissionBase {
        readonly ATCLientModule atc;
        BoxInfo BoxCurrent;
        BoxInfo BoxTarget;
        public CBoxMission(ShipControllerModule aController, ATCLientModule aClient, BoundingSphereD aSphere) : base(aController, aSphere) {
            atc = aClient;
        }

        public override void Update() {
            base.Update();
            BoxCurrent = atc.GetBoxInfo(Volume.Center);
            
            
        }
    }
}
