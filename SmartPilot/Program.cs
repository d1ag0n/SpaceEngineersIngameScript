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
        readonly LCDModule mLCD;
        
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            mController = new ShipControllerModule();
            mThrust = new ThrusterModule();
            mGyro = new GyroModule();
            mLCD = new LCDModule();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, p => ModuleManager.Accept(p));
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource) {
            if ("r" == argument) {
                var t = mThrust.Get(0);
                if (t != null) {
                    if (mThrust.Remove(t)) {
                        g.persist($"{t.CustomName} removed");
                    } else {
                        g.persist($"{t.CustomName} not removed");
                    }
                }
            }
            try {
                mThrust.Update();
                mGyro.Rotate(Vector3D.Down);
            } catch(Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            Echo(str);
            mLCD.WriteAll(str);
        }
    }
}
