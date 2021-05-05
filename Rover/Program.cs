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
        readonly ShipControllerModule mController;
        readonly ModuleManager mManager;
        readonly ThrustModule mThrust;
        readonly GyroModule mGyro;
        IMyThrust thrustL, thrustR;
        public Program() {
            thrustL = GridTerminalSystem.GetBlockWithName("MyThrust Left") as IMyThrust;
            thrustR = GridTerminalSystem.GetBlockWithName("MyThrust Right") as IMyThrust;
            mManager = new ModuleManager(this, "Rover", "logConsole", 10);
            mController = new ShipControllerModule(mManager);
            mGyro = new GyroModule(mManager);
            mGyro.Down = true;
            mThrust = new ThrustModule(mManager);
            mThrust.Damp = false;
            mManager.Initialize();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save() {
        }
        
        public void Main(string argument, UpdateType updateSource) {
            try {

                mManager.Update(argument, updateSource);
                mSpeed.Update(mController.LinearVelocity);
                lighter();
                info();
                mManager.mLog.log($"Lthrust{thrustL.ThrustOverridePercentage:f2}");
                mManager.mLog.log($"Rthrust{thrustR.ThrustOverridePercentage:f2}");
            } catch (Exception ex) {
                Echo(ex.ToString());
            }
        }
        readonly Lag mSpeed = new Lag(60);
        void info() {
            mManager.mLog.log($"Mass: {mController.Mass:f0}");
            mManager.mLog.log($"Gravity: {Vector3D.Normalize(mController.GravityLocal):f2}");
        }
        Vector3D vz => Vector3D.Zero;
        const double speedEpsilon = 5;
        void lighter() {
            mThrust.Emergency = true;
            var fact = 0f;
            if (MAF.nearEqual(mSpeed.Value, 0, speedEpsilon)) {
                mManager.mLog.log("Zeroing out");
                var accel = vz;
                if (mController.Cockpit.MoveIndicator.Z < 0) {
                    mManager.mLog.log("GOT MOVE");
                    accel += Base6Directions.GetVector(mController.Cockpit.Orientation.Forward) * 10;
                }
                mThrust.Acceleration = accel;
                mGyro.SetTargetDirection(vz);
            } else {
                if (MAF.angleBetween(mController.Gravity, mController.Cockpit.WorldMatrix.Down) > 0.262) {
                    mGyro.SetTargetDirection(mController.Gravity);
                } else {
                    mGyro.SetTargetDirection(vz);
                }

                if (mController.Cockpit.HandBrake) {
                    mManager.mLog.log("Handbrake on");
                    //mThrust.Acceleration = MAF.world2dir(-mGravity * 0.9, Me.CubeGrid.WorldMatrix) + -mController.LocalLinearVelo;
                    mThrust.Acceleration = Vector3D.Normalize(mController.GravityLocal) * 1.5 - mController.LocalLinearVelo;
                } else {
                    var pct = (MathHelper.Clamp(mSpeed.Value, speedEpsilon, speedEpsilon + 10) - speedEpsilon) / 10;
                    var accel = mController.GravityLocal * 0.9 * pct;
                    if (mController.Cockpit.MoveIndicator.Z < 0) {
                        mManager.mLog.log("GOT MOVE");
                        accel += Base6Directions.GetVector(mController.Cockpit.Orientation.Forward);
                    }
                    mManager.mLog.log("Handbrake off");

                    mThrust.Acceleration = accel;
                }
            }
            mManager.mLog.log($"Lighter {fact:f2}");

        }
    }
}
