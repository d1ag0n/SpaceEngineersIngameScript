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

        readonly Logger g;
        readonly GTS gts;

        readonly List<IMyTextPanel> mLCDs = new List<IMyTextPanel>();
        //readonly List<RotoFinger> fingers = new List<RotoFinger>();
        //readonly RotoFinger firstFinger;
        readonly List<MyDetectedEntityInfo> mDetected = new List<MyDetectedEntityInfo>();
        //RotoFinger lastFinger;
        //Vector3D mvTarget = new Vector3D(13115.33, 139870.21, -105437.42);

        readonly IMyShipController rc;


        //GPS:dead:13115.33:139870.21:-105437.42:#FF75C9F1:

        

        float initialAngle = 1;

        readonly RotoArm arm;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);

            gts.initList(mLCDs);

            

            IMyMotorAdvancedStator stator = null;
            gts.getByTag("arm", ref stator);
            if (stator != null) {
                arm = new RotoArm(stator, gts, g);
            }
            gts.getByTag("armcontrol", ref rc);
            
        }


        public void Save() { }
       
        public void Main(string argument, UpdateType updateSource) {
            try {
                if (rc != null) {
                    g.log(arm.ModifyTarget(rc.MoveIndicator));
                }
                arm.Update();
            } catch (Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            foreach (var lcd in mLCDs) lcd.WriteText(str);
            Echo(str);
        }
    }
}
