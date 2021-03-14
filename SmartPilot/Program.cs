using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        main update;
        delegate void main(string a, UpdateType u);
        //double runtimes = 0;
        //int timesrun = 0;
        //readonly LogModule g;
        //readonly ThrusterModule mThrust;
        //readonly GyroModule mGyro;
        
        //readonly SensorModule mSensor;
        CameraModule mCamera;
        readonly Lag mLag = new Lag(90);
        
        PeriscopeModule mPeriscope;
        MenuModule mMenu;
        string slast = "nothing";
        public Program() {
            ModuleManager.Initialize(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            if (max < Runtime.LastRunTimeMs) {
                max = Runtime.LastRunTimeMs;
                ModuleManager.logger.persist("in constructor " + slast + max);
            }
            //ModuleManager.GetModule(out g);
            //mSensor = new SensorModule();
            //mThrust = new ThrusterModule();
            //mGyro = new GyroModule();
            update = (a, u) => {
                if (max < Runtime.LastRunTimeMs) {
                    max = Runtime.LastRunTimeMs;
                    ModuleManager.logger.persist("in initializer " + slast + max);
                }
                new GyroModule();
                mCamera = new CameraModule();
                mPeriscope = new PeriscopeModule();
                mMenu = new MenuModule();
                update = load;
                slast = "initializer";
            };
            slast = "constructor";
        }

        void load(string a, UpdateType u) {
            if (max < Runtime.LastRunTimeMs) {
                max = Runtime.LastRunTimeMs;
                ModuleManager.logger.persist("in loader " + slast + max);
            }
            try {
                ModuleManager.Load(Storage);
            } catch (Exception ex) {
                ModuleManager.logger.persist(ex.ToString());
            }
            update = loop;
            slast = "loader";
        }
        int loopcount = 0;

        void loop(string arg, UpdateType type) {
            
            if (max < Runtime.LastRunTimeMs) {
                max = Runtime.LastRunTimeMs;
                ModuleManager.logger.persist(loopcount + " loop " + slast + max);
            }
            loopcount++;
            var lag = mLag.update(Runtime.LastRunTimeMs);
            try {
                ModuleManager.logger.log("cur ", Runtime.LastRunTimeMs);
                ModuleManager.logger.log("lag ", lag);
                ModuleManager.logger.log("max  ", max);
                if (arg.Length > 0) {
                    if (arg == "save") {
                        Save();
                    } else {
                        mMenu.Input(arg);
                    }
                }
                ModuleManager.Update();
            } catch (Exception ex) {
                ModuleManager.logger.persist(ex.ToString());
            }
            slast = "loop";
        }

        public void Save() {
            Me.CustomData = Storage = ModuleManager.Save();
        }
        double max = 0;
        public void Main(string a, UpdateType u) => update(a, u);
    }
}
