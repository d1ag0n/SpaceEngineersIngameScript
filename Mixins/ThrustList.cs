using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    
    public class ThrustList : BlockDirList<IMyThrust> {
        //public Vector3D Acceleration;
        // these provide acceleration in the respective direction
        public double LeftForce => forces[2];
        public double RightForce => forces[3];
        public double UpForce => forces[4];
        public double DownForce => forces[5];
        public double FrontForce => forces[0];
        public double BackForce => forces[1];
        double[] forces = new double[6];


        // Forward = 0,
        // Backward = 1,
        // Left = 2,
        // Right = 3,
        // Up = 4,
        // Down = 5
        public void Update(ref Vector3D aAccel, double aMass, bool emergency = false) {
            int f = 0, b = 1, l = 2, r = 3, u = 4, d = 5;
            if (aAccel.X < 0) {
                r = 2; l = 3;
            }
            if (aAccel.Y > 0) {
                u = 5; d = 4;
            }
            if (aAccel.Z < 0) {
                f = 1; b = 0;
            }

            handleLists(aMass, ref aAccel.X, l, r, emergency);
            handleLists(aMass, ref aAccel.Y, u, d, emergency);
            handleLists(aMass, ref aAccel.Z, f, b, emergency);
            
        }

        void handleLists(double aMass, ref double aAccel, int aUse, int aDisable, bool emergency) {

            runList(aMass, ref aAccel, aUse, emergency);
            var forceSum = 0.0;
            foreach (var t in mLists[aDisable]) {
                if (t.Enabled) t.Enabled = false;
                forceSum += t.MaxEffectiveThrust;
            }
            forces[aDisable] = forceSum;
        }

        void runList(double aMass, ref double aAccel, int aList, bool emergency) {
            var A = Math.Abs(aAccel);
            var F = aMass * A;
            //ModuleManager.logger.log($"F = {F}");
            //ModuleManager.logger.log($"M = {aMass}");
            //ModuleManager.logger.log($"A = {A}");
            var forceSum = 0.0;
            foreach (var t in mLists[aList]) {
                double met = t.MaxEffectiveThrust;
                forceSum += met;
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
            forces[aList] = forceSum;
        }
    }
}
