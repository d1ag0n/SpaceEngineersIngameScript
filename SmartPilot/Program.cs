using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        //double runtimes = 0;
        //int timesrun = 0;
        //readonly LogModule g;
        //readonly ThrusterModule mThrust;
        //readonly GyroModule mGyro;
        
        //readonly SensorModule mSensor;
        readonly CameraModule mCamera;
        readonly Lag mLag = new Lag(90);
        
        readonly PeriscopeModule mPeriscope;
        readonly MenuModule mMenu;
        public Program() {
            ModuleManager.Initialize(this);
            //ModuleManager.GetModule(out g);
            

            //mSensor = new SensorModule();
            //mThrust = new ThrusterModule();
            //mGyro = new GyroModule();
            mCamera = new CameraModule();
            mPeriscope = new PeriscopeModule();
            mMenu = new MenuModule();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save() { }
        Vector3D dir = Vector3D.Down;
        public void Main(string argument, UpdateType updateSource) {
            var lag = mLag.update(Runtime.LastRunTimeMs);

            try {
                ModuleManager.logger.log("main ", lag);
                if (argument.Length > 0) {
                    mMenu.Input(argument);
                }
                MyDetectedEntityInfo e;
                /*if (mSensor.Player(out e)) {
                    //dir = e.Position - mController.WorldMatrix.Translation;
                }*/
                ModuleManager.Update();
            } catch(Exception ex) {
                ModuleManager.logger.persist(ex.ToString());
            }
            
        }
    }
}
