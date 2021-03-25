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
        
        public CBoxMission(ShipControllerModule aController, ATCLientModule aClient, BoundingSphereD aSphere) : base(aController, aSphere) {
            atc = aClient;
        }



        public override void Update() {
            base.Update();
            BoxCurrent = atc.GetBoxInfo(Volume.Center);
            if (BoxCurrent.IsReservedBy(ctr.EntityId)) {
                ctr.logger.log("Box is reserved ", BoxCurrent.Position);
            } else {
                atc.Reserve(BoxCurrent);
                ctr.logger.log("Acquiring reservation ", BoxCurrent.Position);
            }
        }
    }
}
