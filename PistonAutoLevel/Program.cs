using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        [Flags]
        enum Modes {
            stop = 0,
            retract = 1,
            level = 2
        }
        Modes mMode = Modes.stop;
        public class Leg {
            Program mProgram;
            static int count = 1;
            IMyPistonBase mPiston;
            IMyLandingGear mGear;
            public Leg(Program p, IMyPistonBase b) {
                mProgram = p;
                b.CustomName = $"Leg {count}";
                count++;
                mPiston = b;
                List<IMyTerminalBlock> list;
                if (p.mGrids.TryGetValue(b.TopGrid.EntityId, out list)) {
                    foreach (var g in list) {
                        if (mGear == null) {
                            mGear = g as IMyLandingGear;
                        }
                    }
                }
                init();
            }
            void init() {
                mPiston.Enabled = true;
                mPiston.MinLimit = mPiston.LowestPosition;
                mPiston.MaxLimit = mPiston.HighestPosition;
                mGear.Enabled = true;
                mGear.AutoLock = false;
            }
            void unlock() {
                if (mGear.LockMode == LandingGearMode.Locked) {
                    mGear.Unlock();
                }
            }
            public bool Retract() {

                unlock();
                mPiston.Velocity = -1f;
                return nearEqual(mPiston.CurrentPosition, mPiston.MinLimit);
            }
            public bool Touch() {
                unlock();
                if (mGear.LockMode == LandingGearMode.ReadyToLock) {
                    mPiston.Velocity = 0f;
                    return true;
                } else {
                    mPiston.Velocity = 0.6f;
                }
                return false;
            }
            public bool Lock() {
                mGear.Lock();
                return mGear.IsLocked;
            }
            public void Stop() {
                mPiston.Velocity = 0f;
            }
            public bool Level(IMyShipController sc) {
                var gearPos = mGear.WorldMatrix.Translation;
                var scPos = sc.CenterOfMass;
                var gravNormal = Vector3D.Normalize(sc.GetNaturalGravity());
                var posOnSCPlane = orthoProject(gearPos, scPos, sc.WorldMatrix.Down);
                var posOnGPlane = orthoProject(gearPos, scPos, gravNormal);
                var dist = (float)(posOnGPlane - posOnSCPlane).Length();
                

                var gear2gp = (posOnGPlane - gearPos).Length();
                var gear2scp = (posOnSCPlane - gearPos).Length();

                if (gear2gp > gear2scp) {
                    
                } else {
                    //mPiston.Velocity = -0.05f;
                }
                mPiston.Velocity = (float)(gear2gp - gear2scp);
                mProgram.sb.AppendLine($"{mPiston.CustomName}: dist={dist:f2}, gear2gp={gear2gp:f2},gear2scp={gear2scp:f2}, velo={mPiston.Velocity}");
                if (dist <= 0.26f) {
                    return true;
                }
                return false;
            }
            Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - ((aTarget - aPlane).Dot(aNormal) * aNormal);
            bool nearEqual(double a, double b, double epsilon = 0.01) =>
            Math.Abs(a - b) < epsilon;
        }
        StringBuilder sb = new StringBuilder();
        IMyTextPanel mLCD;
        IMyShipController mRemote;
        List<Leg> mLegs = new List<Leg>();
        List<IMyTerminalBlock> mBlocks = new List<IMyTerminalBlock>();
        Dictionary<long, List<IMyTerminalBlock>> mGrids = new Dictionary<long, List<IMyTerminalBlock>>();
        public Program() {
            GridTerminalSystem.GetBlocksOfType(mBlocks, addByGrid);
            var bg = GridTerminalSystem.GetBlockGroupWithName("Level");
            if (bg != null) {
                int i = 0;
                bg.GetBlocks(null, b => {
                    i++;

                    if (b is IMyPistonBase) {
                        mLegs.Add(new Leg(this, b as IMyPistonBase));
                    } else {
                        Echo(b.ToString());
                    }
                    return false;
                });
            }
        }
        bool addByGrid(IMyTerminalBlock b) {
            if (mRemote == null) {
                mRemote = b as IMyShipController;
            }
            if (mLCD == null) {
                mLCD = b as IMyTextPanel;
            }
            List<IMyTerminalBlock> list;
            if (!mGrids.TryGetValue(b.CubeGrid.EntityId, out list)) {
                list = new List<IMyTerminalBlock>();
                mGrids.Add(b.CubeGrid.EntityId, list);
            }
            list.Add(b);
            return true;
        }

        bool stop() {
            foreach (var leg in mLegs) {
                leg.Stop();
            }
            return true;
        }
        bool level() {
            if (doLevel()) {
                stop();
                return touch();
            }
            return false;
        }

        bool doLevel() {
            bool result = true;
            foreach (var leg in mLegs) {
                if (!leg.Level(mRemote)) {
                    result = false;
                }
            }
            return result;
        }

        bool touch() {
            bool result = true;
            foreach (var leg in mLegs) {
                if (!leg.Touch()) {
                    result = false;
                }
            }
            return result;
        }
        bool retract() {
            var result = true;
            foreach (var leg in mLegs) {
                if (!leg.Retract()) {
                    result = false;
                }
            }
            return result;
        }
        bool doLock() {
            var result = true;
            foreach (var leg in mLegs) {
                if (!leg.Lock()) {
                    result = false;
                }
            }
            return result;
        }

        public void Main(string argument, UpdateType updateSource) {
            if (argument == null)
                return;
            argument = argument.Trim().ToLower();
            if (argument == "stop") {
                mMode = Modes.stop;
            } else if (argument == "retract") {
                mMode = Modes.retract;
            } else if (argument == "level") {
                mMode = Modes.level;
            }
            if ((mMode & (Modes.level | Modes.retract)) != 0) {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            } else {
            }
            bool result = true;

            switch (mMode) {
                case Modes.stop:
                    result = stop();
                    break;
                case Modes.level:
                    result = level();
                    if (result) {
                        result = doLock();
                    }
                    break;
                case Modes.retract:
                    result = retract();
                    break;
            }

            if (result) {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                mMode = Modes.stop;
                Echo("done");
            } else {
                Echo("working");
            }
            var str = sb.ToString();
            if (str.Length > 0) {
                //mLCD.WriteText(str);
            }
            sb.Clear();
        }
    }
}
