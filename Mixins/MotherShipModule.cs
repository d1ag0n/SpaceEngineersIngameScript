using System;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using VRage;


namespace IngameScript {
    public class MotherShipModule : Module<IMyTerminalBlock> {
        readonly GridComModule mCom;
        readonly ShipControllerModule mController;

        
        public MotherShipModule(ModuleManager aManager) : base(aManager) {
            //mListener = ModuleManager.Program.IGC.RegisterBroadcastListener("Register");
            aManager.GetModule(out mCom);
            aManager.GetModule(out mController);
            onUpdate = UpdateAction;
            Active = true;
        }

        public override bool Accept(IMyTerminalBlock aBlock) => false;
        DateTime lastUpdate = DateTime.MinValue;
        void UpdateAction() {
            if (mController.Remote == null) {
                return;
            }
            var time = DateTime.Now;
            if ((time - lastUpdate).TotalSeconds > 1.0) {
                var v = mController.ShipVelocities.LinearVelocity;
                var s = 0.0;
                if (!v.IsZero()) {
                    s = v.Normalize();
                }
                var m = Grid.WorldMatrix;
                var t = MotherState.Pack(Grid.WorldAABB, v, s, mController.ShipVelocities.AngularVelocity, Grid.WorldMatrix, mController.Remote.CenterOfMass);
                mManager.mProgram.IGC.SendBroadcastMessage("MotherState", t);
                mLog.log("Sent");
            } else {
                mLog.log("Waiting to send");
            }
        }


    }
}
