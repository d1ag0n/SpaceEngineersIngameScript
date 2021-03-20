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
        
        
        readonly IMyCameraBlock cam;
        readonly IMyTextPanel lcd;
        readonly IMyShipController pit;
        
        readonly List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);
            gts.get(ref pit);
            gts.get(ref lcd);
            gts.initList(cameras);
            scanner = new Scanner(cameras, g);
            gts.getByTag("CamMain", ref cam);
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            var e = new MyDetectedEntityInfo();

            g.log("scanning");
            var scan = new Vector3D(14042, 130347.41, -106800.42);
            var pc = new Vector3D(13449.54, 130159, -107030.33);
            
            if (scanner.Scan(scan, ref e)) {
                g.log(e.Type);
                if (e.Type != MyDetectedEntityType.None) {
                    g.log(g.gps("hit", e.HitPosition.Value));
                }
            }

            var str = g.clear();
            Echo(str);
            lcd.WriteText(str);
        }
    }
}
