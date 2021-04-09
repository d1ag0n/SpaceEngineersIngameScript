using System;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using VRage;


namespace IngameScript {
    public class MotherShipModule : Module<IMyTerminalBlock> {
        readonly GridComModule mCom;
        

        
        public MotherShipModule(ModuleManager aManager) : base(aManager) {
            //mListener = ModuleManager.Program.IGC.RegisterBroadcastListener("Register");
            aManager.GetModule(out mCom);
            
            onUpdate = UpdateAction;
        }

        public override bool Accept(IMyTerminalBlock aBlock) => false;
        DateTime lastUpdate = DateTime.MinValue;
        void UpdateAction() {

            var time = DateTime.Now;
            if ((time - lastUpdate).TotalSeconds > 1.0) {
                var v = controller.ShipVelocities.LinearVelocity;
                var s = 0.0;
                if (!v.IsZero()) {
                    s = v.Normalize();
                }
                var m = Grid.WorldMatrix;
                var t = MotherState.Pack(Grid.WorldAABB, v, s, controller.ShipVelocities.AngularVelocity, Grid.WorldMatrix);
                mManager.mProgram.IGC.SendBroadcastMessage("MotherState", t);
            }
        }


    }
}
