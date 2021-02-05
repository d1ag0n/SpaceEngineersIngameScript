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
        readonly GTS gts;
        readonly Logger g;
        //readonly IMyCameraBlock cam;
        readonly IMyTextPanel lcd;
        readonly IMyShipController pit;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            g = new Logger();
            gts = new GTS(this, g);
            gts.get(ref pit);
            gts.get(ref lcd);
            
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {


            g.log(pit.CalculateShipMass().BaseMass);
            g.log(pit.CalculateShipMass().PhysicalMass);
            g.log(pit.CalculateShipMass().TotalMass);
            
            var str = g.clear();
            Echo(str);
            lcd.WriteText(str);
        }
    }
}
