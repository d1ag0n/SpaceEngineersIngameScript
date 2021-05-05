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
        const string PISTON_GROUP = "PISTONS";
        readonly List<IMyPistonBase> mPistons = new List<IMyPistonBase>();
        readonly List<IMyShipDrill> mDrills = new List<IMyShipDrill>();
        readonly IMyShipController mController;
        readonly IMyTextPanel mLCD;
        public Program() {
            var bg = GridTerminalSystem.GetBlockGroupWithName(PISTON_GROUP);
            if (bg != null) {
                bg.GetBlocksOfType(mPistons);
            }
            var sc = mController;
            var s = mLCD;
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, b => {
                if (sc == null) {
                    sc = b as IMyShipController;
                }
                var d = b as IMyShipDrill;
                if (d != null) {
                    mDrills.Add(d);
                }
                if (s == null) {
                    if (b.CustomData.Contains("#slopeDisplay")) {
                        s = b as IMyTextPanel;
                    }
                }
                return false;
            });
            mController = sc;
            mLCD = s;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save() {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }
        Vector3D mTarget;
        double mDepth = 50;
        double mRadius = 50;
        double mAngle;
        bool started;
        bool set;
        bool run;
        bool ready = false;
        readonly List<MyWaypointInfo> mWaypoint = new List<MyWaypointInfo>();
        Vector3D drillPos() {
            var i = 0d;
            var pos = Vector3D.Zero;
            foreach (var d in mDrills) {
                i += 1d;
                pos += d.WorldMatrix.Translation;
            }
            return pos / i;
        }
        public void Main(string argument, UpdateType updateSource) {
            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
                //try {
                if (argument == "target") {
                    set = true;
                    ready = started = run = false;
                    mTarget = drillPos() + Vector3D.Normalize(mController.GetNaturalGravity()) * mDepth;
                } else if (argument.StartsWith("depth")) {
                    if (!double.TryParse(argument.Substring(argument.IndexOf(' ')).Trim(), out mDepth)) {
                        mDepth = 50;
                    }
                } else if (argument == "cancel") {
                    ready = started = run = false;
                    retract();
                    drills(false);
                } else if (argument == "start") {
                    if (ready || started) {
                        started = run = true;
                    }
                } else if (argument == "stop") {
                    run = false;
                    drills(false);
                    retract();
                }
                //} catch { }
            }
            if ((updateSource & (UpdateType.Update10)) != 0) {

                if (!run) {
                    drills(false);
                    retract();
                }
                if (set == false) {
                    return;
                }
                var g = Vector3D.Normalize(mController.GetNaturalGravity());
                var pos = drillPos();

                var drillProj = MAF.orthoProject(pos, mTarget, g);
                var disp2drillProj = drillProj - mTarget;
                var edgeDir = disp2drillProj;
                var projDistFromTarget = edgeDir.Normalize();

                var edge = mTarget + edgeDir * mRadius;

                var edgeProj = MAF.orthoProject(edge, pos, g);
                var disp2edge = edge - pos;
                var disp2proj = edgeProj - pos;
                var ab = MAF.angleBetween(disp2edge, disp2proj);

                if (disp2edge.Dot(mController.WorldMatrix.Down) < 0) {
                    //ab = -ab;
                }
                ready = false;
                if (projDistFromTarget > mRadius) {
                    if (!started) {
                        ready = true;
                        mAngle = ab;
                    }
                } else {
                    ab = MathHelper.PiOver2;
                }

                double altitude;
                mController.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude);

                var piston = 0f;
                if (run) {
                    if (MAF.nearEqual(ab, mAngle, 0.035)) {
                        drills(true);
                    } else {
                        drills(false);
                    }
                    piston = (float)(ab - mAngle);
                    pistons(piston);
                }
                mLCD.WriteText(
                    $"Angle {MathHelper.ToDegrees(ab):f1}\nAltitude {altitude:f1}\nPiston {piston:f4}"
                );
            }
        }
        void retract() {
            pistons(-0.1f);
        }
        void pistons(float velo) {
            velo *= 100;
            velo = MathHelper.Clamp(velo, -0.1f, 0.1f);
            foreach (var p in mPistons) {
                p.Velocity = velo;
            }
        }
        void drills(bool enabled) {
            foreach(var d in mDrills) {
                d.Enabled = enabled;
            }
        }
        string gps2str(string aName, Vector3D aPos) {
            // GPS:ARC_ABOVE:19680.65:144051.53:-109067.96:#FF75C9F1:
            var sb = new StringBuilder("GPS:");
            sb.Append(aName);
            sb.Append(":");
            sb.Append(aPos.X.ToString("F2"));
            sb.Append(":");
            sb.Append(aPos.Y.ToString("F2"));
            sb.Append(":");
            sb.Append(aPos.Z.ToString("F2"));
            sb.Append(":#FFFF00FF:");
            return sb.ToString();
        }
    }
}
