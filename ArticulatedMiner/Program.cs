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
        const float MAX_TURN_ANGLE = 0.4f;
        const float MAX_BEND_ANGLE = 0.4f;
        const string WHEEL_RIGHT = "Front Right";
        const string WHEEL_LEFT = "Front Left";
        IMyShipController sc = null;
        IMyMotorAdvancedStator mTurn, mBend;
        bool mTurnReverse, mBendReverse;
        bool hold = true;
        readonly Dictionary<long, List<IMyTerminalBlock>> mGridBlocks = new Dictionary<long, List<IMyTerminalBlock>>();
        public Program() {
            
            mGridBlocks.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, addByGrid);
            var list = getByGrid(Me.CubeGrid.EntityId);
            foreach (var b in list) {
                if (sc == null) {
                    sc = b as IMyShipController;
                } else {
                    break;
                }
            }
            foreach (var b in list) {
                if (b is IMyMotorAdvancedStator) {
                    //Echo("name:" + b.CustomName);
                    var bup = b.Orientation.Up;
                    var scleft = sc.Orientation.Left;
                    var scup = sc.Orientation.Up;
                    if (bup == scleft) {
                        //Echo("Bend Up");
                        mBendReverse = false;
                    } else if (bup == Base6Directions.GetOppositeDirection(scleft)) {
                        //Echo("Bend Down");
                        mBendReverse = true;
                    } else if (bup == scup) {
                        //Echo("Turn Right");
                        mTurn = b as IMyMotorAdvancedStator;
                        mTurnReverse = false;
                        break;
                    } else if (bup == Base6Directions.GetOppositeDirection(scup)) {
                        //Echo("Turn Left");
                        mTurn = b as IMyMotorAdvancedStator;
                        mTurnReverse = true;
                        break;
                    } else {
                        Echo($"bup{bup}, scleft{scleft}, scup{scup}");
                    }

                } else {
                    Echo(b.ToString());
                }
            }
            if (mTurn == null) {
                findTurn(getByGrid(mBend.Top.CubeGrid.EntityId));
                findWheels(mTurn.TopGrid);
            } else {
                findBend(getByGrid(mTurn.Top.CubeGrid.EntityId));
                findWheels(mBend.TopGrid);
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        IMyMotorSuspension mWheelLeft, mWheelRight;
        void findWheels(IMyCubeGrid grid) {
            var list = getByGrid(grid.EntityId);

            foreach (var b in list) {
                if (b.CustomName == WHEEL_LEFT) {
                    mWheelLeft = b as IMyMotorSuspension;
                } else if (b.CustomName == WHEEL_RIGHT) {
                    mWheelRight = b as IMyMotorSuspension;
                }
            }
            
        }
        public void Save() {
        }
        bool addByGrid(IMyTerminalBlock b) {
            List<IMyTerminalBlock> list;
            if (!mGridBlocks.TryGetValue(b.CubeGrid.EntityId, out list)) {
                list = new List<IMyTerminalBlock>();
                mGridBlocks.Add(b.CubeGrid.EntityId, list);
            }
            list.Add(b);
            return false;
        }
        List<IMyTerminalBlock> getByGrid(long entityId) {
            List<IMyTerminalBlock> result;
            if (!mGridBlocks.TryGetValue(entityId, out result)) {
                result = null;
            }
            return result;
        }

        public void Main(string argument, UpdateType updateSource) {
            if (argument == "hold") {
                hold = !hold;
            }
            try {
                if (sc.MoveIndicator.Z != 0) {
                    if (sc.MoveIndicator.Z < 0) {
                        mWheelLeft.ApplyAction("IncreasePropulsion override");
                        mWheelRight.ApplyAction("IncreasePropulsion override");
                    } else {
                        mWheelLeft.ApplyAction("DecreasePropulsion override");
                        mWheelRight.ApplyAction("DecreasePropulsion override");
                    }
                } else {
                    mWheelLeft.ApplyAction("ResetPropulsion override");
                    mWheelRight.ApplyAction("ResetPropulsion override");
                }
                if (sc.MoveIndicator.X != 0 || sc.MoveIndicator.Y != 0) {

                    var b = MathHelper.Clamp(sc.MoveIndicator.Y, -0.1f, 0.1f);
                    if (mBendReverse) {
                        b *= -1;
                    }
                    mBend.TargetVelocityRad = b;
                    if (Math.Abs(mBend.Angle) > MAX_BEND_ANGLE) {
                        mBend.TargetVelocityRad = mBend.Angle * -0.01f;
                    }

                    var t = MathHelper.Clamp(sc.MoveIndicator.X, -0.1f, 0.1f);
                    if (mTurnReverse) {
                        t *= -1;
                    }
                    mTurn.TargetVelocityRad = t;
                    if (Math.Abs(mTurn.Angle) > MAX_TURN_ANGLE) {
                        mTurn.TargetVelocityRad = mTurn.Angle * -0.01f;
                    }
                } else {

                    if (hold) {
                        mTurn.TargetVelocityRad = -mTurn.Angle * 0.5f;
                        mBend.TargetVelocityRad = -mBend.Angle * 0.5f;
                    } else {
                        if (Math.Abs(mTurn.Angle) > MAX_TURN_ANGLE) {
                            mTurn.TargetVelocityRad = mTurn.Angle * -0.01f;
                        } else {
                            mTurn.TargetVelocityRad = 0;
                        }
                        if (Math.Abs(mTurn.Angle) > MAX_BEND_ANGLE) {

                        } else {
                            mBend.TargetVelocityRad = 0;
                        }
                    }
                }
            } catch (Exception ex) {
                try{ mTurn.TargetVelocityRad = 0;} catch { }
                try{ mBend.TargetVelocityRad = 0;} catch { }
                
            }
        }

        void findBend(List<IMyTerminalBlock> list) {
            
            foreach (var b in list) {
                if (b is IMyMotorAdvancedStator) {
                    if (b.Orientation.Forward == mTurn.Top.Orientation.Up) {
                        mBend = b as IMyMotorAdvancedStator;
                        if (mTurnReverse) {
                            mBendReverse = true;
                            //Echo("1 Bend Reverse true");
                        } else {
                            mBendReverse = false;
                            //Echo("1 Bend Reverse false");
                        }
                        break;
                    } else if (b.Orientation.Forward == Base6Directions.GetOppositeDirection(mTurn.Top.Orientation.Up)) {
                        mBend = b as IMyMotorAdvancedStator;
                        if (mTurnReverse) {
                            mBendReverse = false;
                            //Echo("2 Bend Reverse false");
                        } else {
                            mBendReverse = true;
                            //Echo("2 Bend Reverse true");
                        }
                        break;
                    }

                }
            }
        }
        void findTurn(List<IMyTerminalBlock> list) {

        }
    }
}
