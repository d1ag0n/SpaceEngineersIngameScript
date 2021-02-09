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
        readonly IMyGyro gyro;
        readonly IMyTextPanel lcd;
        readonly IMyShipController rc;
        readonly Logger g;
        readonly GTS gts;
        double time = 0;
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);
            gts.get(ref gyro);
            gts.get(ref lcd);
            gts.get(ref rc);
        }

        public void Save() {

        }
        string str = "running";
        public void Main(string argument, UpdateType updateSource) {
            time += Runtime.TimeSinceLastRun.TotalSeconds;
            
            if (!enable && time > 10.0) {
                gyro.Enabled = false;
                if (endTime == 0) {
                    
                    endTime = time;
                    var sv = rc.GetShipVelocities();
                    var sm = rc.CalculateShipMass();
                    g.log("velo " + sv.AngularVelocity.Length().ToString());
                    g.log("mass " + sm.PhysicalMass.ToString());
                    g.log("time " + endTime.ToString());
                    str = g.clear();
                }
                
            }
            
            lcd.WriteText(str);
            Echo(str);
        }
    }
}
