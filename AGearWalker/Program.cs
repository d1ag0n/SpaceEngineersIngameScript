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
        readonly IMyShipController mPit;
        readonly List<IMyLandingGear> mGear = new List<IMyLandingGear>();
        readonly List<Leg> mLeft = new List<Leg>();
        readonly List<Leg> mRight = new List<Leg>();
        class Leg {
            public bool Left = false;
            public readonly IMyMotorStator stator;
            public IMyPistonBase piston { get; private set; }
            IMyShipController mPit;
            float offset;
            public Leg(IMyShipController aPit, IMyMotorStator aStator, string Name) {
                mPit = aPit;
                stator = aStator;
                stator.CustomName = Name;
                stator.ShowOnHUD = true;
            }
            const float pistonSpeed = 2.5f;
            public bool step(bool step) {
                if (step) {
                    piston.Velocity = piston.MaxLimit - piston.CurrentPosition;
                    return MAF.nearEqual(piston.MaxLimit, piston.CurrentPosition, 0.5);
                } else {
                    piston.Velocity = piston.MinLimit - piston.CurrentPosition;
                    return MAF.nearEqual(piston.MinLimit, piston.CurrentPosition, 0.5);
                }
            }
            public bool Add(IMyPistonBase p) {
                if (p.CubeGrid == stator.TopGrid) {
                    piston = p;
                    var ab = MAF.angleBetween(stator.WorldMatrix.Right, mPit.WorldMatrix.Forward);
                    var o = offset = 0f;
                    if (Left) {

                        if (ab > 3) {
                            o += MathHelper.Pi;
                        } else if (ab > 1) {
                            if (stator.WorldMatrix.Left.Dot(mPit.WorldMatrix.Forward) > 0) {
                                o += MathHelper.PiOver2;
                            } else {
                                o -= MathHelper.PiOver2;
                            }
                        }
                        stator.CustomName += $" {o:f2}";

                        var ppos = Base6Directions.GetClosestDirection(MAF.world2pos(p.WorldMatrix.Translation, stator.Top.WorldMatrix));
                        switch (ppos) {
                            case Base6Directions.Direction.Right:
                                offset += MathHelper.PiOver2;
                                break;
                            case Base6Directions.Direction.Left:
                                offset -= MathHelper.PiOver2;
                                break;
                            case Base6Directions.Direction.Forward:
                                offset += MathHelper.Pi;
                                break;
                        }
                        stator.CustomName += $" {offset:f2}";

                    } else {
                        if (ab > 3) {
                            o += MathHelper.Pi;
                        } else if (ab > 1) {
                            if (stator.WorldMatrix.Left.Dot(mPit.WorldMatrix.Forward) > 0) {
                                o -= MathHelper.PiOver2;
                            } else {
                                o += MathHelper.PiOver2;
                            }
                        }
                        stator.CustomName += $" {o:f2}";

                        var ppos = Base6Directions.GetClosestDirection(MAF.world2pos(p.WorldMatrix.Translation, stator.Top.WorldMatrix));
                        switch (ppos) {
                            case Base6Directions.Direction.Right:
                                offset += MathHelper.PiOver2;
                                break;
                            case Base6Directions.Direction.Left:
                                offset -= MathHelper.PiOver2;
                                break;
                            case Base6Directions.Direction.Backward:
                                offset += MathHelper.Pi;
                                break;
                        }
                        stator.CustomName += $" {offset:f2}";
                    }
                    
                    offset += o;
                    return true;
                }
                return false;
            }
            const float bend = 0.6f;
            public bool forward() => setAngle(Left ? -bend : bend);
            public bool backward() => setAngle(Left ? bend : -bend);
            public bool zero() => setAngle(0);

            bool setAngle(float aAngle) {
                aAngle += offset;
                aAngle -= stator.Angle;
                MathHelper.LimitRadians(ref aAngle);


                if (aAngle > MathHelper.Pi) {
                    aAngle -= MathHelper.TwoPi;
                }

                bool result = Math.Abs(aAngle) < 0.2;
                if (result) {
                    stator.TargetVelocityRad = 0;
                } else {
                    stator.TargetVelocityRad = aAngle;
                }
                return result;
            }
        }
        public Program() {
            GridTerminalSystem.GetBlocksOfType(mGear);
            foreach (var g in mGear) {
                g.AutoLock = false;
                g.Enabled = true;
                g.Unlock();
            }

            var pit = mPit;
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(null, b => {
                if (pit == null) {
                    pit = b as IMyShipController;
                }
                return false;
            });
            mPit = pit;
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(null, b => {
                var pos = MAF.world2pos(b.WorldMatrix.Translation, mPit.WorldMatrix);
                if (pos.Dot(Vector3D.Left) > 0) {
                    var l = new Leg(mPit, b, "Left");
                    l.Left = true;
                    mLeft.Add(l);
                } else {
                    mRight.Add(new Leg(mPit, b, "Right"));
                }
                return false;
            });
            sort(mLeft, "Left");
            sort(mRight, "Right");
            GridTerminalSystem.GetBlocksOfType<IMyPistonBase>(null, b => {
                foreach (var l in mLeft) {
                    if (l.Add(b)) {
                        return false;
                    }
                }
                foreach (var r in mRight) {
                    if (r.Add(b)) {
                        return false;
                    }
                }
                return false;
            });
            

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        void sort(List<Leg> list, string name) {
            list.Sort((a, b) => MAF.world2pos(a.stator.WorldMatrix.Translation, mPit.WorldMatrix).Z < MAF.world2pos(b.stator.WorldMatrix.Translation, mPit.WorldMatrix).Z ? -1 : 1);
            int i = 1;
            foreach (var l in list) {
                l.stator.CustomName = $"{name} {i++}";
            }
        }

        bool left = false;
        bool zero = false;

        public void Main(string argument, UpdateType updateSource) {
            if (!zero) {
                zero = true;
                foreach (var l in mLeft) {
                    if (!l.zero()) {
                        zero = false;
                    }
                }
                foreach (var r in mRight) {
                    if (!r.zero()) {
                        zero = false;
                    }
                }
            }
            int m = 1;
            if (left) {
                m = 0;
            }
            bool result = true;
            for (int i = 0; i < mLeft.Count; i++) {
                var l = mLeft[i];
                var r = mRight[i];
                if (i % 2 == m) {
                    if (l.step(true) && r.step(false)) {
                        
                        if (r.forward() & l.backward()) {

                        } else {
                            Echo("1 no move");
                            result = false;
                        }
                    } else {
                        Echo("1 no step");
                        result = false;
                    }
                } else {
                    if (r.step(true) && l.step(false)) {
                        if (r.backward() & l.forward()) {
                        } else {
                            Echo("2 no move");
                            result = false;
                        }
                    } else {
                        Echo("2 no step");
                        result = false;
                    }
                }
            }
            if (result) {
                left = !left;
            }
            Echo($"result={result}");
        }
    }
}
