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
        Vector3D mvTarget;
        readonly Stator stator;
        readonly Logger g;
        readonly List<IMyTextPanel> mLCDs = new List<IMyTextPanel>();
        readonly IMyRemoteControl mRC;
        readonly List<MyWaypointInfo> waypoints = new List<MyWaypointInfo>();
        readonly IMySensorBlock mSensor;
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            var gts = new GTS(this, g);
            IMyMotorStator block = null;
            gts.get(ref block);
            gts.initList(mLCDs, false);
            stator = new Stator(block, g);
            stator.SpeedFactor = 5.0f;
            gts.get(ref mRC);
            gts.get(ref mSensor);
        }

        public void Save() {
        }
        void procArgument(string argument) {

            waypoints.Clear();
            mRC.GetWaypointInfo(waypoints);
            foreach (var wp in waypoints) {
                if (wp.Name.ToLower() == argument) {
                    stator.SetTarget(wp.Coords);
                    return;
                }
            }
            float angle;
            if (float.TryParse(argument, out angle)) {
                g.persist("angle set @ " + angle);
                stator.SetTarget(angle);
            }
        }
        readonly List<MyDetectedEntityInfo> mDetected = new List<MyDetectedEntityInfo>();
        void doDetect() {
            mSensor.DetectedEntities(mDetected);
            foreach (var e in mDetected) {
                if (e.Type == MyDetectedEntityType.CharacterHuman) {
                    stator.SetTarget(e.Position);
                    break;
                }
            }
            mDetected.Clear();
        }
        public void Main(string argument, UpdateType updateSource) {
            try {
                procArgument(argument);
                doDetect();
                stator.Info();
                stator.Update();
            } catch(Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            foreach(var lcd in mLCDs) { 
                lcd.WriteText(str);
            }
            Echo(str);
        }
    }
}
