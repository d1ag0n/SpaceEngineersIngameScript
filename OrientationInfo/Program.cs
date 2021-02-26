using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        readonly GTS gts;
        readonly Logger g;
        readonly List<IMyCubeBlock> blocks = new List<IMyCubeBlock>();
        readonly List<IMyTextPanel> mLCDs = new List<IMyTextPanel>();
        public Program() {
            g = new Logger();
            gts = new GTS(this, g);
            gts.initList(blocks);
            gts.initList(mLCDs);

            foreach(var b in blocks) {
                log(b);
                if (b is IMyMechanicalConnectionBlock) {
                    var mcb = b as IMyMechanicalConnectionBlock;
                    if (mcb.Top != null) {
                        log(mcb.Top);
                    }
                }
            }

            var str = g.clear();
            foreach(var lcd in mLCDs) {
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                lcd.WriteText(str);
            }
        }
        void log(IMyCubeBlock b) {
            var m = b.WorldMatrix;
            var t = m.Translation;
            var f = m.Forward;
            var u = m.Up;
            g.log(g.gps(b.DisplayNameText + "F", t + f));
            g.log(g.gps(b.DisplayNameText + "U", t + u));
        }

        public void Save() {
        }

        public void Main(string argument, UpdateType updateSource) {
        }
    }
}
