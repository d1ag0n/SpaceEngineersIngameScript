using System;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using VRage;


namespace IngameScript {
    public class MotherShipModule : Module<IMyTerminalBlock> {
        
        public MotherShipModule(ModuleManager aManager) : base(aManager) {
            //mListener = ModuleManager.Program.IGC.RegisterBroadcastListener("Register");
            aManager.mIGC.SubscribeBroadcast("MotherState", onMotherState);
            onUpdate = UpdateAction;
        }
        void onMotherState(IGC.Envelope e) {
            MotherId = e.Message.Source;
            logger.log("MotherId ", MotherId);
            var ms = MotherShipModule.MotherState(e.Message.Data);
            MotherBox = ms.Item1;
            MotherVeloDir = ms.Item2;
            MotherSpeed = ms.Item3;
            logger.log("MotherSpeed ", MotherSpeed);
            MotherAngularVelo = ms.Item4;
            MotherMatrix = ms.Item5;
            MotherCoM = ms.Item6;
            MotherLastUpdate = e.Time;
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
                var t = MotherState(Grid.WorldAABB, v, s, controller.ShipVelocities.AngularVelocity, Grid.WorldMatrix);
                mManager.mProgram.IGC.SendBroadcastMessage("MotherState", t);
            }
        }

        public MyTuple<BoundingBoxD, Vector3D, double, Vector3D, MatrixD, Vector3D> 
            MotherState(BoundingBoxD volume, Vector3D dir, double speed, Vector3D ngVelo, MatrixD orientation) =>
            MyTuple.Create(volume, dir, speed, ngVelo, orientation, controller.Remote.CenterOfMass);
        public static MyTuple<BoundingBoxD, Vector3D, double, Vector3D, MatrixD, Vector3D> MotherState(object data) => 
            (MyTuple<BoundingBoxD, Vector3D, double,Vector3D, MatrixD, Vector3D>)data;
    }
}
