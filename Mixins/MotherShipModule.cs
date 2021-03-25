using System;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using VRage;

namespace IngameScript {
    public class MotherShipModule : Module<IMyTerminalBlock> {
        
        public MotherShipModule() {
            //mListener = ModuleManager.Program.IGC.RegisterBroadcastListener("Register");
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
                var t = MotherState(ModuleManager.WorldVolume, v, s);
                ModuleManager.Program.IGC.SendBroadcastMessage("MotherState", t);
            }
        }

        public static MyTuple<BoundingSphereD, Vector3D, double> MotherState(BoundingSphereD position, Vector3D dir, double speed) => MyTuple.Create(position, dir, speed);
        public static MyTuple<BoundingSphereD, Vector3D, double> MotherState(object data) => (MyTuple<BoundingSphereD, Vector3D, double>)data;
    }
}
