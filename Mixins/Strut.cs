using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {
    public class Strut {
        
        readonly RotorMirror mMain;
        readonly RotorMirror mSub;
        readonly Program mProgram;
        readonly IMyMotorSuspension mWheel;
        readonly float mDir;
        readonly List<IMyMotorSuspension> mWheels = new List<IMyMotorSuspension>();
        public Strut(Program aProgram, IMyMotorStator aStator, float aDir) {
            mProgram = aProgram;
            mMain = new RotorMirror(aStator);
            mDir = aDir;
            var list = aProgram.getByGrid(aStator.TopGrid);
            
            foreach (var b in list) {
                if (b.CustomName == Program.SUB_SUSPENSION) {
                    mSub = new RotorMirror(b as IMyMotorStator);
                    mWheel = getWheel();
                    break;
                }
            }
            if (mSub == null) {
                throw new Exception("WTF");
            }
        }
        IMyMotorSuspension getWheel() {
            var list = mProgram.getByGrid(mSub.mStator.TopGrid);
            IMyMotorSuspension result = null;
            foreach (var b in list) {
                var w = b as IMyMotorSuspension;
                if (w != null) {
                    initWheel(w);
                    if (w.CustomName == Program.SUB_SUSPENSION) {
                        result = w;
                    }
                }
            }
            return result;
        }
        void initWheel(IMyMotorSuspension w) {
            w.AirShockEnabled = true;
            w.Brake = false;
            w.Enabled = true;
            w.Friction = 95;
            w.Height = -1.5f;
            w.Propulsion = true;
            w.Steering = true;
            w.MaxSteerAngle = MathHelper.ToRadians(30);
            w.Strength = 99.9f;
            w.Power = 99.9f;
            mWheels.Add(w);
            //throw new Exception($"{w.CustomName} {w.GetValueFloat("Propulsion override")}");
        }
        public bool debug = true;
        void log(string s) {
            if (debug)
                mProgram.Echo(s);
        }
        int lastX = -2;
        int lastZ = -11;
        public void Update(Vector3 mi) {
            var proj = MAF.orthoProject(mSub.mStator.WorldMatrix.Translation, mMain.mStator.WorldMatrix.Translation, mProgram.mController.WorldMatrix.Up);
            var disp2sub = mSub.mStator.WorldMatrix.Translation - mMain.mStator.WorldMatrix.Translation;
            var disp2proj = proj - mMain.mStator.WorldMatrix.Translation;
            var ab = MAF.angleBetween(disp2proj, disp2sub);
            var dot = disp2sub.Dot(mProgram.mController.WorldMatrix.Up);

            if (dot > 0) {
                log("Above");

            } else {
                ab = -ab;
                log("Below");
            }
            log($"base ab={ab}");
            ab += MathHelper.PiOver4;

            if (mDir > 0) {
                dot = disp2sub.Dot(mProgram.mController.WorldMatrix.Forward);
            } else {
                dot = disp2sub.Dot(mProgram.mController.WorldMatrix.Backward);
            }
            if (dot < 0) {
                ab -= MathHelper.PiOver4;
            }

            log($"ab={ab}");
            
            var angle = mMain.mStator.Angle - (float)(mDir * ab);
            log($"angle={angle}");
            mMain.setAngle(angle);
            ab = MAF.angleBetween(mWheel.WorldMatrix.Backward, mProgram.mGravity);
            dot = mWheel.WorldMatrix.Right.Dot(mProgram.mGravity);
            if (dot > 0) {
                ab = -ab;
            }
            mSub.setVelocity((float)ab);

            int x = 0;
            if (mi.X < 0) {
                x = -1;
            } else if (mi.X > 0) {
                x = 1;
            }
            if (lastX != x) {
                lastX = x;
                var so = x * mDir;
                foreach (var w in mWheels) {
                    w.SetValue("Steer override", so);
                }
            }
            int z = lastZ;
            
            if (mi.Y > 0) {
                z = 0;
            } else {
                if (mi.Z < 0) {
                    z--;
                } else if (mi.Z > 0) {
                    z++;
                }
            }
            if (lastZ != z) {
                lastZ = z;
                foreach (var w in mWheels) {
                    var po = z * 0.1f;
                    w.SetValue("Propulsion override", po);
                }
            }
        }
    }
}
