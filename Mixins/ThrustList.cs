using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    
    public class ThrustList : BlockDirList<IMyThrust> {
        //public Vector3D Acceleration;
        // these provide acceleration in the respective direction
        public double LeftForce;
        public double RightForce;
        public double UpForce;
        public double DownForce;
        public double FrontForce;
        public double BackForce;

        public void Update(ref Vector3D aAccel, double aMass, bool emergency = false) {
            pickList(aMass, ref aAccel.X, mLeft, mRight, ref LeftForce, ref RightForce, emergency);
            pickList(aMass, ref aAccel.Y, mDown, mUp, ref DownForce, ref UpForce, emergency);
            pickList(aMass, ref aAccel.Z, mFront, mBack, ref FrontForce, ref BackForce, emergency);
            
        }

        void pickList(double aMass, ref double aAccel, List<IMyThrust> aNeg, List<IMyThrust> aPos, ref double aFNeg, ref double aFPos, bool emergency) {
            var o = aNeg;
            var swap = false;
            if (aAccel > 0) {
                swap = true;
                aNeg = aPos;
                aPos = o;
            }
            var F1 = runList(aMass, ref aAccel, aNeg, emergency);
            var F2 = 0.0;
            foreach (var t in aPos) {
                if (t.Enabled) t.Enabled = false;
                F2 += t.MaxEffectiveThrust;
            }
            if (swap) {
                aFPos = F1;
                aFNeg = F2;
            } else {
                aFNeg = F1;
                aFPos = F2;
            }
        }

        double runList(double aMass, ref double aAccel, List<IMyThrust> aList, bool emergency) {
            var A = Math.Abs(aAccel);
            var F = aMass * A;
            //ModuleManager.logger.log($"F = {F}");
            //ModuleManager.logger.log($"M = {aMass}");
            //ModuleManager.logger.log($"A = {A}");
            var result = 0.0;
            foreach (var t in aList) {
                double met = t.MaxEffectiveThrust;
                result += met;
                if (A > 0 && (met / t.MaxThrust > 0.75 || (emergency && t.MaxEffectiveThrust > 0))) {
                    if (t.IsFunctional) {
                        if (!t.Enabled) t.Enabled = true;
                        // 2 < 1 = false;
                        if (met < F) {
                            t.ThrustOverridePercentage = 1;
                            F -= met;
                            if (aAccel > 0) {
                                aAccel -= met / aMass;
                            } else {
                                aAccel += met / aMass;
                            }
                        } else {
                            t.ThrustOverridePercentage = (float)(F / met);
                            F = 0;
                            aAccel = 0;
                        }
                    }
                } else {
                    if (t.Enabled) t.Enabled = false;
                    t.ThrustOverride = 0;
                }
            }
            return result;
        }
    }
}
