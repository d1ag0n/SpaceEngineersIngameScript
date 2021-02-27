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
        readonly List<RotoFinger> fingers = new List<RotoFinger>();
        readonly RotoFinger firstFinger;
        readonly IMySensorBlock mSensor;
        readonly List<MyDetectedEntityInfo> mDetected = new List<MyDetectedEntityInfo>();
        RotoFinger lastFinger;
        Vector3D mvTarget = new Vector3D(13115.33, 139870.21, -105437.42);
        //GPS:dead:13115.33:139870.21:-105437.42:#FF75C9F1:

        bool walkComplete = true;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);
            gts.initList(mLCDs);
            gts.get(ref mSensor);
            IMyMotorAdvancedStator stator = null;
            gts.getByTag("arm", ref stator);
            if (stator != null) {
                //mvTarget = stator.WorldMatrix.Translation + stator.WorldMatrix.Up * 1000.0;
                firstFinger = lastFinger = new RotoFinger(stator, g, gts);
                //firstFinger.SetTarget(mvTarget);
                firstFinger.SetTurnTarget(0);
                firstFinger.SetBendTarget(0.1f);
                fingers.Add(firstFinger);
                walkComplete = false;
            }
        }

        void doDetect() {
            if (mSensor != null) {
                mSensor.DetectedEntities(mDetected);
                foreach (var e in mDetected) {
                    if (e.Type == MyDetectedEntityType.CharacterHuman) {
                        mvTarget = e.Position;
                        break;
                    }
                }
                mDetected.Clear();
            }
        }

        public void Save() { }
        void doWalk() {
            RotoFinger finger;
            if (!walkComplete) {
                finger = lastFinger.nextFinger;
                if (finger == null) {
                    walkComplete = true;
                    g.persist("walk complete found " + fingers.Count + " fingers");
                } else if (finger.okay) {
                    lastFinger = finger;
                    fingers.Add(finger);
                    //finger.SetTarget(mvTarget);
                    finger.SetBendTarget(0.1f);
                    finger.SetTurnTarget(0);
                } else {
                    walkComplete = true;
                    g.persist("FINGER WAS NOT OKAY");
                }
            }
        }
        void procArg(string arg) {
            float angle;
            if (arg == "up") {
                if (firstFinger != null) {
                    mvTarget = firstFinger.stator.WorldMatrix.Translation + firstFinger.stator.WorldMatrix.Up * 1000.0;
                    foreach (var f in fingers) {
                        f.SetTarget(mvTarget);
                    }
                }
            } else if (float.TryParse(arg, out angle)) {
                foreach (var f in fingers)
                    f.SetBendTarget(angle);
            }
        }
        public void Main(string argument, UpdateType updateSource) {
            try {
                doWalk();
                procArg(argument);
                if (walkComplete) {
                    //doDetect();

                    foreach (var f in fingers) {
                        f.Update();
                    }
                }
            } catch (Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            foreach (var lcd in mLCDs) lcd.WriteText(str);
            Echo(str);
        }
    }
}
