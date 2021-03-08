using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    class LCDManager : Module<IMyTextPanel> {
        readonly List<IMyTextPanel> mLCDs = new List<IMyTextPanel>();
        readonly Logger g;
        public LCDManager() {
            GetModule(out g);
        }
        public override bool Accept(IMyTerminalBlock b) {
            var result = base.Accept(b);
            if (result) {
                var p = b as IMyTextPanel;
                mLCDs.Add(p);
                p.ContentType = ContentType.TEXT_AND_IMAGE;
            }
            return result;
        }

        public override bool Remove(IMyTerminalBlock b) {
            var result = base.Remove(b);
            if (result) {
                mLCDs.Remove(b as IMyTextPanel);
            }
            return result;
        }

        public void WriteAll(string str) {
            foreach (var p in mLCDs) {
                p.WriteText(str);
            }
        }
    }
}
