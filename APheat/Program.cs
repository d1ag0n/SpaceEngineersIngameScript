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
        readonly List<Hip> mHips = new List<Hip>();
        readonly Dictionary<long, List<IMyTerminalBlock>> mGridBlocks = new Dictionary<long, List<IMyTerminalBlock>>();
        public readonly IMyRemoteControl mRemote;
        public class Hip {
            readonly float down = 0;
            readonly float up = 0;
            IMyMotorStator mStator;
            IMyLandingGear mGear;

            public Hip(Program p, IMyMotorStator s) {
                
                mStator = s;
                var list = p.getByGrid(s.TopGrid);
                if (list != null) {
                    foreach (var b in list) {
                        if (mGear == null) {
                            mGear = b as IMyLandingGear;
                        }
                    }
                }
                var cn = s.CustomName;
                var io = cn.IndexOf(':');
                if (io > -1) {
                    cn = cn.Substring(io + 1);
                }
                down = float.Parse(cn);
                if (down < 0) {
                    down = Math.Abs(down);
                    up = MathHelper.ToRadians(down + 135);
                } else {
                    up = MathHelper.ToRadians(down - 135);
                }
                down = MathHelper.ToRadians(down);
            }
            public void goUp() => setStatorAngle(up);
            public void goDown() => setStatorAngle(down);
            void setStatorAngle(float aAngle) {
                aAngle -= mStator.Angle;
                MathHelper.LimitRadians(ref aAngle);
                if (aAngle > MathHelper.Pi) {
                    aAngle -= MathHelper.TwoPi;
                }
                mStator.TargetVelocityRad = aAngle;
            }
            public static Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
                aTarget - ((aTarget - aPlane).Dot(aNormal) * aNormal);
        }

        public Program() {
            bool.TryParse(Storage, out down);
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, addByGrid);
            var list = getByGrid(Me.CubeGrid);
            foreach (var b in list) {
                if (mRemote == null) {
                    mRemote = b as IMyRemoteControl;
                } else {
                    break;
                }
            }
            foreach (var b in list) {
                var r = b as IMyMotorStator;
                if (r != null) {
                    mHips.Add(new Hip(this, r));
                }
            }
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
        bool down = false;
        public void Main(string argument, UpdateType updateSource) {
            if (argument == "up") {
                down = false;
            } else if (argument == "down") {
                down = true;
            } else if (argument == "toggle") {
                down = !down;
            }
            Storage = down.ToString();
            Echo($"hips: {mHips.Count}");
            foreach (var h in mHips) {
                if (down) {
                    h.goDown();
                } else {
                    h.goUp();
                }
            }
        }
    }
}
