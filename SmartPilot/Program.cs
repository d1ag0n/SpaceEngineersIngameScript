using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    partial class Program : MyGridProgram {
        readonly ThrustManager mThrust;
        readonly Logger g;
        readonly LCDManager mLCD;
        readonly List<IAccept> mModules = new List<IAccept>();
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            mThrust = new ThrustManager(g);
            mLCD = new LCDManager(g);
            mModules.Add(mThrust);
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, p => {
                foreach (var m in mModules) m.Accept(p); return false; 
            });
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
            } catch(Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            Echo(str);
            mLCD.WriteAll(str);
        }
    }
}
