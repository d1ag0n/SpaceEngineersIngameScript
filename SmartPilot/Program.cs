using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        readonly LogModule g;
        readonly ThrusterModule mThrust;
        readonly GyroModule mGyro;
        readonly ShipControllerModule mController;
        readonly SensorModule mSensor;
        readonly Lag mLag = new Lag(90);
        
        readonly PeriscopeModule mPeriscope;
        readonly MenuModule mMenu;
        public Program() {
            ModuleManager.Initialize(this);
            ModuleManager.GetModule(out g);
            ModuleManager.GetModule(out mController);

            mSensor = new SensorModule();
            mThrust = new ThrusterModule();
            mGyro = new GyroModule();
        
            mPeriscope = new PeriscopeModule();
            mMenu = new MenuModule();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        double runtimes = 0;
        int timesrun = 0;
        public void Save() { }
        Vector3D dir = Vector3D.Down;
        public void Main(string argument, UpdateType updateSource) {
            var lag = mLag.update(Runtime.LastRunTimeMs);
            timesrun++;
            if (argument.Length > 0) {
                mMenu.Input(argument);
            }
            switch (argument) {
                case "r":
                    dir = MAF.ranDir();
                    break;
                case "up":
                    dir = Vector3D.Up;
                    break;
                case "down":
                    dir = Vector3D.Down;
                    break;
                case "left":
                    dir = Vector3D.Left;
                    break;
                case "right":
                    dir = Vector3D.Right;
                    break;
                case "front":
                    dir = Vector3D.Forward;
                    break;
                case "back":
                    dir = Vector3D.Backward;
                    break;
            }
            try {
                ModuleManager.logger.log("main ", lag);
                MyDetectedEntityInfo e;
                if (mSensor.Player(out e)) {
                    //dir = e.Position - mController.WorldMatrix.Translation;
                }
                ModuleManager.Update();
            } catch(Exception ex) {
                g.persist(ex.ToString());
            }
            
        }
    }
}
