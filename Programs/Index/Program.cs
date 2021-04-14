using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Index {
    class Program {
        /*
 * R e a d m e
 * -----------
 * add to the END of the Name of 4 Pistons 
 * P_FL
 * P_FR
 * P_BL
 * P_BR
 * 
 * add to the END of the Name a remote control block
 * 
 * P_CTRL
 * 
 * you can also change the refresh rate if you are unhappy with the speed of the leveling do this by chaning: 
 * 
 * //Runtime.UpdateFrequency = UpdateFrequency.Update100; 100=number of ticks between each update, setting this to 10 makes the leveling VERY smooth comparativly 
 * 
 * 
 * You should Adjust the pistons so that they lift the grid off of the ground befor begining the leveling process, do this by editing this fuction:
 * 
 * //pfl.MaxLimit = 0.9F;    change 0.9 on the 4 lines to the minimum value the pistons need to get to in order to lift the grid off of the surface it is sitting on
 * 
 * Special thanks to whiplash for me using some of his old code in here and helping me out
 * Also thanks to Malware, d1ag0n and the rest of the discord community for helping out!
 */


        IMyRemoteControl Remote;
        IMyPistonBase pfl;
        IMyPistonBase pfr;
        IMyPistonBase pbl;
        IMyPistonBase pbr;

        bool Capture(IMyTerminalBlock Block) {
            if (Block.CustomName.EndsWith("P_FL"))
                pfl = Block as IMyPistonBase;
            else if (Block.CustomName.EndsWith("P_FR"))
                pfr = Block as IMyPistonBase;
            else if (Block.CustomName.EndsWith("P_BL"))
                pbl = Block as IMyPistonBase;
            else if (Block.CustomName.EndsWith("P_BR"))
                pbr = Block as IMyPistonBase;
            else if (Block.CustomName.EndsWith("P_CTRL"))
                Remote = Block as IMyRemoteControl;

            return false;
        }
        public Program() {
            {
                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(null, Capture);
            }
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
        }
        enum Mode {
            None, Up, Down, Right, Left, Stop
        }
        Mode mode = Mode.None;

        public void Main(string argument, UpdateType updateSource) {
            Echo(mode.ToString());

            double roll, pitch;
            getRotationAnglesFromDown(Remote.WorldMatrix, Remote.GetNaturalGravity(), out pitch, out roll);

            Echo(MathHelper.ToDegrees(roll).ToString());
            Echo(MathHelper.ToDegrees(pitch).ToString());
            Echo(roll.ToString());
            Echo(pitch.ToString());
            if (argument == "Start") {
                mode = Mode.None;
                pfl.Velocity = Math.Abs(pfl.Velocity);
                pfr.Velocity = Math.Abs(pfl.Velocity);
                pbl.Velocity = Math.Abs(pfl.Velocity);
                pbr.Velocity = Math.Abs(pfl.Velocity);
            } else if (argument == "Stop") {
                mode = Mode.Stop;
            }
            if (mode != Mode.Stop) {
                if (roll < -0.03) {
                    mode = Mode.Left;
                } else if (roll > 0.03) {
                    mode = Mode.Right;
                } else if (pitch < -0.03) {
                    mode = Mode.Down;
                } else if (pitch > 0.03) {
                    mode = Mode.Up;
                } else {
                    mode = Mode.Stop;
                }
                Echo(pfl.CurrentPosition.ToString());
                Echo(pfr.CurrentPosition.ToString());
                Echo(pbl.CurrentPosition.ToString());
                Echo(pbr.CurrentPosition.ToString());

            }
            if (argument == "Reset") {
                mode = Mode.Stop;
                pfl.Velocity = -Math.Abs(pfl.Velocity);
                pfr.Velocity = -Math.Abs(pfl.Velocity);
                pbl.Velocity = -Math.Abs(pfl.Velocity);
                pbr.Velocity = -Math.Abs(pfl.Velocity);
                pfl.MaxLimit = 0.9F;
                pfr.MaxLimit = 0.9F;
                pbl.MaxLimit = 0.9F;
                pbr.MaxLimit = 0.9F;

            }
            if (mode == Mode.Right) {
                if (Math.Abs(pfl.CurrentPosition - pfl.MaxLimit) < 0.05 && Math.Abs(pbl.CurrentPosition - pbl.MaxLimit) < 0.05) {
                    pfl.MaxLimit = pfl.MaxLimit + Math.Abs((float)roll);
                    pbl.MaxLimit = pbl.MaxLimit + Math.Abs((float)roll);
                }
            } else if (mode == Mode.Left) {
                if (Math.Abs(pfr.CurrentPosition - pfr.MaxLimit) < 0.05 && Math.Abs(pbr.CurrentPosition - pbr.MaxLimit) < 0.05) {
                    pfr.MaxLimit = pfr.MaxLimit + Math.Abs((float)roll);
                    pbr.MaxLimit = pbr.MaxLimit + Math.Abs((float)roll);
                }
            } else if (mode == Mode.Up) {
                if (Math.Abs(pfr.CurrentPosition - pfr.MaxLimit) < 0.05 && Math.Abs(pfl.CurrentPosition - pfl.MaxLimit) < 0.05) {
                    pfr.MaxLimit = pfl.MaxLimit + Math.Abs((float)pitch);
                    pbr.MaxLimit = pfr.MaxLimit + Math.Abs((float)pitch);
                }
            } else if (mode == Mode.Down) {
                if (Math.Abs(pbr.CurrentPosition - pbr.MaxLimit) < 0.05 && Math.Abs(pbl.CurrentPosition - pbl.MaxLimit) < 0.05) {
                    pbl.MaxLimit = pbl.MaxLimit + Math.Abs((float)pitch);
                    pbr.MaxLimit = pbr.MaxLimit + Math.Abs((float)pitch);
                }
            }
        }
        public static double angleBetween(Vector3D a, Vector3D b) {
            double result = 0;
            if (!Vector3D.IsZero(a) && !Vector3D.IsZero(b))
                result = Math.Acos(MathHelperD.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));

            return result;
        }
        static void getRotationAnglesFromDown(MatrixD world, Vector3D targetVector, out double pitch, out double roll) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(world));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);


            pitch = angleBetween(Vector3D.Down, flattenedTargetVector);
            if (localTargetVector.Z > 0)
                pitch = -pitch;

            roll = angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X > 0)
                roll = -roll;

        }

    }
    internal class ProgramBase {

    }
}
