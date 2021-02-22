using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        readonly IMyTextPanel lcd;
        readonly Logger g;
        readonly GTS gts;
        readonly Gyro gy;
        readonly Lag lag = new Lag(6);
        double time = 0;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);
            gts.get(ref lcd);
            gy = new Gyro(gts, g);
        }

        public void Save() {

        }
        Vector3D downTo;
        void process(string arg) {
            if (null != arg) {
                int dir;
                if (int.TryParse(arg, out dir)) {
                    if (dir == 0) {
                        downTo = Vector3D.Zero;
                    } else {
                        dir = MathHelper.Clamp(--dir, 0, 5);
                        downTo = Base6Directions.Directions[dir];
                    }
                }
            }
        }
        public void Main(string arg, UpdateType update) {
            if ((update & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
                process(arg);
            } else {
                g.log(lag.update(Runtime.LastRunTimeMs));
                gy.Rotate(downTo);
                
                var str = g.clear();
                lcd.WriteText(str);
                Echo(str);
            }
        }
    }
}
