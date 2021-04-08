using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;

namespace IngameScript {
    public class GyroAVMission : MissionBase {
        
        readonly ShipControllerModule mController;
        readonly GyroModule mGyro;
        readonly LogModule mLog;

        Vector3D configDir = Vector3D.Up;
        int configCount = 0;

        public GyroAVMission(ModuleManager aManager) : base(aManager) {
            aManager.GetModule(out mController);
            aManager.GetModule(out mGyro);
            aManager.GetModule(out mLog);
            mGyro.Active = true;
        }
        public override bool Cancel() => true;

        public override void Input(string arg) { }

        public override void Update() {
            mLog.log("config update");
            if (mController.ShipVelocities.AngularVelocity.LengthSquared() < 1 && MAF.angleBetween(mController.Remote.WorldMatrix.Forward, configDir) < 0.01) {
                mLog.log("config waiting");
                configCount++;
                if (configCount == 18) {
                    configDir = -configDir;
                }
            } else {
                mLog.log("config turning");
                configCount = 0;
                mGyro.SetTargetDirection(configDir);
            }
        }
    }
}
