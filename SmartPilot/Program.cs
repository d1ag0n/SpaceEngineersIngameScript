using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        readonly Logger g;
        readonly ThrusterModule mThrust;
        readonly GyroModule mGyro;
        readonly ShipControllerModule mController;
        readonly SensorModule mSensor;
        readonly LCDModule mLCD;
        
        public Program() {
            ModuleManager.Initialize(this);

            g = new Logger();
            mSensor = new SensorModule();
            mController = new ShipControllerModule();
            mThrust = new ThrusterModule();
            mGyro = new GyroModule();
            mLCD = new LCDModule();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save() { }
        Vector3D dir = Vector3D.Down;
        public void Main(string argument, UpdateType updateSource) {
            
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
                MyDetectedEntityInfo e;
                if (mSensor.Player(out e)) {
                    dir = e.Position - mController.WorldMatrix.Translation;
                }
                mThrust.Update();
                mGyro.Rotate(dir);
            } catch(Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            Echo(str);
            mLCD.WriteAll(str);
        }
    }
}
