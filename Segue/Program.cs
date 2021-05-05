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
        static MyGridProgram instance;
        public static void Echo(string s) => instance.Echo(s);
        IMyShipController pit;
        List<IMyGyro> mGyros = new List<IMyGyro>();
        readonly ThrustList mThrust = new ThrustList();
        List<IMyThrust> list = new List<IMyThrust>();

        public Program() {
            instance = this;
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, b => {
                var g = b as IMyGyro;
                if (g != null) {
                    mGyros.Add(g);
                    return false;
                }
                var t = b as IMyThrust;
                if (t != null) {
                    list.Add(t);
                    mThrust.Add(t);
                }
                if (pit == null) {
                    pit = b as IMyShipController;
                }
                
                return false;
            });

            init();
        }
        void applyGyroOverride(MatrixD worldMatrix, double pitch, double yaw, double roll) {

            //pitch_speed = pidPitch.Control(pitch_speed);
            //roll_speed = pidRoll.Control(roll_speed);

            // Large gyro 3.36E+07
            // Small gyro 448000
            var rotationVec = new Vector3D(-pitch, yaw, roll); //because keen does some weird stuff with signs             

            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var gyro in mGyros) {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyro.WorldMatrix));
                //g.log("transformedRotationVec", transformedRotationVec);

                gyro.Pitch = (float)transformedRotationVec.X;
                gyro.Yaw = (float)transformedRotationVec.Y;
                gyro.Roll = (float)transformedRotationVec.Z;
            }
            //g.log("gyro", transformedRotationVec);

        }
        bool enabled = true;
        bool fly = false;
        public void Save() {
        }
        void init() {
            foreach (var g in mGyros) {
                g.Enabled = true;
                g.Yaw = 0;
                g.Pitch = 0;
                g.Roll = 0;
                g.GyroOverride = enabled;
            }
            mThrust.AllStop();
            if (enabled && fly) {
                pit.DampenersOverride = false;
                pit.ControlThrusters = false;
            } else {
                pit.DampenersOverride = true;
                pit.ControlThrusters = true;
            }

            if (enabled) {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            } else {
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }

        }
        public void Main(string argument, UpdateType updateSource) {
            foreach (var t in list) {
            }
            double pitch, roll;
            if (argument == "on") {
                enabled = true;
                fly = false;
                init();
            } else if (argument == "off") {
                enabled = false;
                fly = false;
                init();
            } else if (argument == "toggle") {
                enabled = !enabled;
                fly = false;
                init();
            } else if (argument == "fly") {
                fly = !fly;
                enabled = fly;
                init();
            }

            
            var llv = MAF.world2dir(pit.GetShipVelocities().LinearVelocity, Me.CubeGrid.WorldMatrix);
            var g = pit.GetNaturalGravity();
            var lg = MAF.world2dir(g, Me.CubeGrid.WorldMatrix);
            
            var ta = -llv;
            ta += -lg;

            double elevation;
            pit.TryGetPlanetElevation(MyPlanetElevation.Surface, out elevation);
            Echo($"elevation={elevation}");


            if (enabled) {
                if (fly) {
                    Echo("flying");
                    mThrust.Update(ta, pit.CalculateShipMass().PhysicalMass, true);
                } else {
                    Echo("leveling");
                }
                getRotationAnglesFromDown(pit.WorldMatrix, Vector3D.Normalize(pit.GetNaturalGravity()), out pitch, out roll);
                //Echo($"pitch={pitch:f2}, roll={roll:f2}");
                applyGyroOverride(pit.WorldMatrix, pitch * 3, pit.RotationIndicator.Y, roll * 3);
                
            } else {
                Echo("Disabled");
            }
        }
        public static void getRotationAnglesFromDown(MatrixD world, Vector3D targetVector, out double pitch, out double roll) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(world));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = angleBetween(Vector3D.Down, flattenedTargetVector);
            if (localTargetVector.Z > 0)
                pitch = -pitch;

            roll = angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X > 0)
                roll = -roll;
        }
        public static double angleBetween(Vector3D a, Vector3D b) {
            double result = 0;
            if (!Vector3D.IsZero(a) && !Vector3D.IsZero(b))
                result = Math.Acos(MathHelperD.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));

            return result;
        }
    }
}
