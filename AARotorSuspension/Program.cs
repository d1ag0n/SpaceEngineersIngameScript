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
    public partial class Program : MyGridProgram {
        const string PREFIX = "!Suspension - ";
        const string FRONT_RIGHT = PREFIX + "Front Right";
        const string FRONT_LEFT = PREFIX + "Front Left";
        const string BACK_RIGHT = PREFIX + "Back Right";
        const string BACK_LEFT = PREFIX + "Back Left";
        public const float MAX_TORQUE = 1.0f;
        public const float MIN_TORQUE = 0.0f;
        public const float MAX_ANGLE = MathHelper.PiOver2;
        public static float mMaxTorque { get; private set; }

        public const string SUB_SUSPENSION = PREFIX + "Left";

        readonly Dictionary<long, List<IMyTerminalBlock>> mGridBlocks = new Dictionary<long, List<IMyTerminalBlock>>();
        public readonly IMyShipController mController;
        readonly List<Strut> mStruts = new List<Strut>();
        
        public Program() {
            var sc = mController;

            IMyMotorStator fr = null;
            IMyMotorStator fl = null;
            IMyMotorStator br = null;
            IMyMotorStator bl = null;

            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, b => {
                addByGrid(b);
                if (b.CustomName == FRONT_RIGHT) {
                    fr = b as IMyMotorStator;
                    return false;
                }
                if (b.CustomName == FRONT_LEFT) {
                    fl = b as IMyMotorStator;
                    return false;
                }

                if (b.CustomName == BACK_RIGHT) {
                    br = b as IMyMotorStator;
                    return false;
                }
                if (b.CustomName == BACK_LEFT) {
                    bl = b as IMyMotorStator;
                    return false;
                }

                if (sc == null) {
                    if (b.CubeGrid == Me.CubeGrid) {
                        sc = b as IMyShipController;
                    }
                    return false;
                }
                return false;
            });
            var t = fr.Torque;
            fr.Torque = float.MaxValue;
            mMaxTorque = fr.Torque;
            fr.Torque = t;
            mStruts.Add(new Strut(this, fr, 1));
            mStruts.Add(new Strut(this, fl, 1));
            mStruts[1].debug = true;
            mStruts.Add(new Strut(this, br, -1));
            mStruts.Add(new Strut(this, bl, -1));

            mController = sc;
            
            //rm = new RotorMirror(GridTerminalSystem.GetBlockWithName("Test Rotor") as IMyMotorStator);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        public List<IMyTerminalBlock> getByGrid(IMyCubeGrid g) {
            List<IMyTerminalBlock> result;
            if (g == null) {
                result = null;
            } else {
                if (!mGridBlocks.TryGetValue(g.EntityId, out result)) {
                    result = new List<IMyTerminalBlock>();
                    mGridBlocks.Add(g.EntityId, result);
                }
            }
            return result;
        }
        bool addByGrid(IMyTerminalBlock b) {
            var list = getByGrid(b.CubeGrid);
            list.Add(b);
            return false;
        }
        public void Save() {
        }
        float a;
        public Vector3D mGravity;
        public void Main(string argument, UpdateType updateSource) {
            Echo($"torque {mMaxTorque}/{float.MaxValue}");
            mGravity = mController.GetNaturalGravity();
            var mi = mController.MoveIndicator;
            if (mi.X != 0 || mi.Y != 0 || mi.Z != 0) {
                
            }
            mController.CustomData = $"{mController.CustomName} {mi}";
            foreach (var s in mStruts) {
                s.Update(mi);
            }
            a += 0.01f;
            MathHelper.LimitRadians(ref a);
            //rm.setAngle(a);
        }
    }
}
