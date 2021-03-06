﻿using Sandbox.Game.EntityComponents;
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
        // name of the one of the rotors that move the elevator
        const string MOVE_ROTOR_1 = "MOVE1";
        
        // name of the other rotor that moves the elevator
        const string MOVE_ROTOR_2 = "MOVE2";
        
        // name of the rotor that balances the elevator
        const string STAY_ROTOR = "STAY";

        // top speed for elevator rotor in radiand per second
        const float MAXSPEED = 0.1f;

        // factor applied to elevator speed, increse if your elevator is taking too long to lock into a floor
        const float AMP = 2.0f;

        // end of config
        readonly IMyShipController mSC;
        readonly IMyMotorStator mR1;
        readonly IMyMotorStator mR2;
        readonly IMyMotorStator mStay;
        readonly Vector3I mBottom;
        readonly Vector3I mTop;
        readonly int floorHeight;
        readonly int topFloor;
        readonly Vector3D mGrav;
        readonly float torque;
        readonly Vector3D mPerp;
        readonly Vector3D mDown;
        public Program() {
            IMyShipController sc = null;
            IMyMotorStator r1 = null, r2 = null, stay =null;
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, b => {
                var r = b as IMyMotorStator;
                if (r != null && r1 == null && r.CustomName == MOVE_ROTOR_1) {
                    r1 = r;
                    return false;
                }
                if (r != null && r2 == null && r.CustomName == MOVE_ROTOR_2) {
                    r2 = r;
                    return false;
                }
                mR1.GetPosition()
                if (r != null && stay == null && r.CustomName == STAY_ROTOR) {
                    stay = r;
                    return false;
                }
                var s = b as IMyShipController;
                if (s != null && sc == null & s.IsSameConstructAs(Me)) {
                    sc = s;
                }
                return false;
            });
            mSC = sc;
            mR1 = r1;
            mR2 = r2;
            mStay = stay;
            torque = Math.Max(r1.Torque, r2.Torque);
            floorHeight = (r1.Position - r2.Position).Length();
            

            mGrav = mSC.GetNaturalGravity();

            var down = Base6Directions.GetIntVector(Base6Directions.GetClosestDirection(MAF.world2dir(mGrav, Me.CubeGrid.WorldMatrix)));
            mDown = MAF.local2dir(down, Me.CubeGrid.WorldMatrix);
            var part = r1.Top == null ? r2.Top : r1.Top;
            mBottom = findStop(part, down);
            mTop = findStop(part, -down);
            topFloor = (mBottom - mTop).Length() / floorHeight;
            var partUp = Base6Directions.GetIntVector(part.Orientation.Up);
            Vector3I perp;
            Vector3I.Cross(ref down, ref partUp, out perp);
            mPerp = MAF.local2dir(perp, Me.CubeGrid.WorldMatrix);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        Vector3I findStop(IMyAttachableTopBlock b, Vector3I aDir) {
            int i = 1;
            Vector3I result = b.Position;
            var pos = result;
            while (true) {
                pos += aDir;
                if (b.CubeGrid.CubeExists(pos)) {
                    if (i < floorHeight) {
                        return result;
                    }
                    if (i == floorHeight) {
                        i = 0;
                        result = pos;
                    }
                }
                i++;
                if (i > floorHeight) {
                    return result;
                }
            }
        }
        public string gps(string aName, Vector3D aPos) {
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
        public void Save() {
        }
        IMyMotorStator work;
        IMyMotorStator other;
        bool mGoDown = false;
        bool mStop = true;
        
        int floors2move = 1;
        int currentFloor;
        int targetFloor = -1;
        int findFloor() {
            Vector3I pos;
            if (mGrav.Dot(mR1.WorldMatrix.Translation - mR2.WorldMatrix.Translation) < 0) {
                pos = mR2.Top.Position;
            } else {
                pos = mR1.Top.Position;
            }
            pos -= mBottom;
            return pos.Length() / floorHeight;
            
        }

        public void Main(string argument, UpdateType updateSource) {
            int gotoFloor;
            if (int.TryParse(argument, out gotoFloor)) {
                targetFloor = MathHelper.Clamp(gotoFloor, 0, topFloor);
                work = null;
            } else if (argument.StartsWith("up")) {
                targetFloor = -1;
                argument = argument.Substring(2);
                if (!int.TryParse(argument, out floors2move)) {
                    floors2move = 1;
                }
                mGoDown = false;
                mStop = false;
                work = null;
            } else if (argument.StartsWith("down")) {
                argument = argument.Substring(4);
                if (!int.TryParse(argument, out floors2move)) {
                    floors2move = 1;
                }
                work = null;
                mGoDown = true;
                mStop = false;
                targetFloor = -1;
            }
            if (targetFloor > -1) {
                floors2move = Math.Abs(currentFloor - targetFloor);

                if (targetFloor > currentFloor) {
                    mGoDown = false;
                } else {
                    mGoDown = true;
                }
                if (floors2move == 0) {
                    targetFloor = -1;
                } else {
                    mStop = false;
                }
            }
            if (work == null) {
                if (mGoDown) {
                    work = getLowest(out other);
                } else {
                    work = getHighest(out other);
                }
                if (!work.IsAttached) {
                    var w = work;
                    work = other;
                    other = w;
                }
                if (work.IsAttached) {
                    if (other.IsAttached) {
                        currentFloor = findFloor();
                        if (currentFloor == targetFloor) {
                            targetFloor = -1;
                            floors2move = 0;
                            mStop = true;
                        }
                    }
                    if (!mStop) {
                        if (mGoDown) {
                            if (work.Top.Position != mBottom) {
                                if (other.IsAttached) {
                                    other.Detach();
                                }
                            } else {
                                mStop = true;
                            }
                        } else {
                            if (work.Top.Position != mTop) {
                                if (other.IsAttached) {
                                    other.Detach();
                                }
                            } else {
                                mStop = true;
                            }
                        }
                    }
                }
            }
            Vector3D other2work;
            float ab;
            if (mGoDown) {
                other2work = other.WorldMatrix.Translation - work.WorldMatrix.Translation;
                ab = (float)MAF.angleBetween(work.WorldMatrix.Translation - other.WorldMatrix.Translation, -mGrav);
            } else {
                other2work = work.WorldMatrix.Translation - other.WorldMatrix.Translation;
                ab = (float)MAF.angleBetween(work.WorldMatrix.Translation - other.WorldMatrix.Translation, mGrav);
            }
            


            

            var dot = other2work.Dot(mPerp);

            var speed = 1.0f;
            if (dot < 0) {
                speed = -speed;
            }
            speed = ab * speed;
            speed *= AMP;
            speed = MathHelper.Clamp(speed, -MAXSPEED, MAXSPEED);
            if (mStop) {
                speed = 0;
            }
            Echo($"ab={ab:f4}");
            Echo($"work={work.CustomName} attached {work.IsAttached} pending {work.PendingAttachment}");
            Echo($"other={other.CustomName} attached {other.IsAttached} pending {other.PendingAttachment}");
            Echo($"speed={speed}");
            Echo($"mGoDown={mGoDown}");
            Echo($"mStop={mStop}");
            Echo($"currentFloor={currentFloor}");
            var dist = (mStay.WorldMatrix.Translation - MAF.local2pos(mBottom * Me.CubeGrid.GridSize, Me.CubeGrid.WorldMatrix)).Length() / (floorHeight * Me.CubeGrid.GridSize);

            
            
            if (ab < 0.001) {

                if (other.PendingAttachment) {
                    Echo($"Pending {other.CustomName}");
                    if (!mStop) {
                        other.Detach();
                    }
                } else {
                    other.Attach();
                    floors2move--;
                    if (floors2move < 1) {
                        mStop = true;
                    }
                    Echo($"Attching {other.CustomName}");
                }
                work.TargetVelocityRad =
                other.TargetVelocityRad =
                mStay.TargetVelocityRad = speed;
                work = null;
            } else {
                work.TargetVelocityRad = speed;
                other.TargetVelocityRad = 0;
            }
            Echo($"floors2move={floors2move}");
            if (mStay.Top != null) {
                ab = (float)MAF.angleBetween(mStay.Top.WorldMatrix.Backward, mGrav);
                if (ab < 0.01) {
                    ab = 0;
                }
                ab *= AMP;
                ab = MathHelper.Clamp(ab, -MAXSPEED, MAXSPEED);
                
                Echo($"AB stay {ab}");
                dot = mStay.Top.WorldMatrix.Right.Dot(mGrav);
                if (dot > 0) {
                    ab = -ab;
                }
                mStay.TargetVelocityRad = ab - speed;
            }

        }
        IMyMotorStator getLowest(out IMyMotorStator other) {
            IMyMotorStator result;
            other = getHighest(out result);
            return result;
        }
        IMyMotorStator getHighest(out IMyMotorStator other) {
            IMyMotorStator result;
            if ((mR1.WorldMatrix.Translation - mR2.WorldMatrix.Translation).Dot(mGrav) < 0) {
                result = mR1;
                other = mR2;
            } else {
                result = mR2;
                other = mR1;
            }
            return result;
        }
    }
}
