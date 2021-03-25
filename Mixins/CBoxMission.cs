using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class CBoxMission : MissionBase {
        readonly double reserveInterval;
        readonly ATCLientModule atc;
        BoxInfo BoxCurrent;
        DateTime reserveRequest;
        public CBoxMission(ShipControllerModule aController, ATCLientModule aClient, BoundingSphereD aSphere) : base(aController, aSphere) {
            atc = aClient;
        }

        void reserve(BoxInfo b) {
            if ((MAF.Now - reserveRequest).TotalSeconds < reserveInterval) {
                var msg = new ATCMsg();
                msg.Info = b;
                msg.Subject = enATC.Reserve;
                ModuleManager.Program.IGC.SendUnicastMessage(ctr.MotherId, "ATC", msg.Box());
            }
        }

        public override void Update() {
            base.Update();
            BoxCurrent = atc.GetBoxInfo(Volume.Center);
            if (BoxCurrent.IsReservedBy(ctr.EntityId)) {
                ctr.logger.log("Box is reserved ", BoxCurrent.Position);
            } else {
                reserve(BoxCurrent);
                ctr.logger.log("Acquiring reservation ", BoxCurrent.Position);
            }
        }
    }
}
