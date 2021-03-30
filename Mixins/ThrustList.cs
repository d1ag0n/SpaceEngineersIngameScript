using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    
    public class ThrustList : BlockDirList<IMyThrust> {

        double[] forces = new double[6];
        readonly ThrustModule mThrust;
        int xIndex, yIndex, zIndex;
        double xForce, yForce, zForce;

        public ThrustList(ThrustModule aThrust) {
            mThrust = aThrust;
        }

        // todo index based acceleration modification, track how many thrusters currently powered to full

        // Forward = 0,
        // Backward = 1,
        // Left = 2,
        // Right = 3,
        // Up = 4,
        // Down = 5
        public void Update(Vector3D aAccel, double aMass, bool emergency = false) {

            // converting to force here
            var z = aAccel.Z * aMass;
            var x = aAccel.X * aMass;
            var y = aAccel.Y * aMass;
            
            // list indices
            int b = 0, f = 1, l = 2, r = 3, u = 4, d = 5;
            /*if (aAccel.Z < 0) {
                f = 1; b = 0;
            }
            if (aAccel.X < 0) {
                r = 2; l = 3;
            }
            if (aAccel.Y > 0) {
                u = 5; d = 4;
            }*/

            if (!emergency) {
                double ratio = forces[f] / z;
                double tempRatio = forces[l] / x;
                if (tempRatio < ratio) {
                    ratio = tempRatio;
                }
                tempRatio = forces[u] / y;
                if (tempRatio < ratio) {
                    ratio = tempRatio;
                }
                if (ratio < 1.0) {
                    z *= ratio;
                    x *= ratio;
                    y *= ratio;
                }
            }

            handleLists(x, ref xForce, ref xIndex, l, r);
            handleLists(y, ref yForce, ref yIndex, u, d);
            handleLists(z, ref zForce, ref zIndex, f, b);
        }
        // got this idea from Elfi not sure if it's what he was planning but..
        // move up/down thrust axes, remember index and force applied
        // onlt hit thrusters required to change the force Sto match
        // copied some of his pattern for this since I was struggling
        void handleLists(double aForce, ref double aCurrent, ref int aIndex, int aUp, int aDown) {
            var delta = aForce - aCurrent;
            int inc = Math.Sign(delta);
            if (aIndex == 0) {
                aIndex += inc;
            }
            while (delta != 0.0) {
                var list = mLists[aIndex > 0 ? aUp : aDown];
                var absIndex = Math.Abs(aIndex);
                var t = list[absIndex - 1];
                var tmax = t.MaxEffectiveThrust;
                var tp = t.ThrustOverridePercentage;
                double change;
                if (delta > 0) {
                    change = Math.Min((1.0 - tp) * tmax, delta);
                    t.ThrustOverridePercentage += (float)change / tmax;
                    delta -= change;
                    aCurrent += change;
                } else {
                    change = Math.Min(tp * tmax, Math.Abs(delta));
                    t.ThrustOverridePercentage += (float)change / tmax;
                    delta += change;
                    aCurrent -= change;
                }
                aIndex += inc;
            }
        }
        public void AllStop() {
            foreach (var list in mLists) {
                foreach (var t in list) {
                    t.Enabled = false;
                }
            }
        }
        public Vector3D MaxAccel(Vector3D aLocalVec, double aMass) {
            //ModuleManager.logger.log("aLocalVec", aLocalVec);
            //ModuleManager.logger.log("aMass ", aMass);
            int f = 0, l = 2, u = 4;
            if (aLocalVec.Z < 0) {
                f = 1;
            }
            if (aLocalVec.X < 0) {
                l = 3;
            }
            if (aLocalVec.Y > 0) {
                u = 5;
            }

            var amp = aLocalVec * 1000.0;
            var z = Math.Abs(amp.Z) * aMass;
            var x = Math.Abs(amp.X) * aMass;
            var y = Math.Abs(amp.Y) * aMass;


            double ratio = forces[f] / z;
            double tempRatio = forces[l] / x;

            if (tempRatio < ratio) {
                ratio = tempRatio;
            }
            tempRatio = forces[u] / y;
            if (tempRatio < ratio) {
                ratio = tempRatio;
            }
            var result = new Vector3D(x, y, z);
            if (ratio < 1.0) {
                result *= ratio;
            }
            result /= aMass;

            if (aLocalVec.Z < 0) {
                result.Z *= -1.0;
            }
            if (aLocalVec.X < 0) {
                result.X *= -1.0;
            }
            if (aLocalVec.Y < 0) {
                result.Y *= -1.0;
            }
            return result;
        }



        double runList(double aForce, int aList) {
            double applied = 0;
            var forceSum = 0.0;
            foreach (var t in mLists[aList]) {
                double met = t.MaxEffectiveThrust;
                //ModuleManager.logger.log("MET ", met);
                forceSum += met;
                if (aForce > 0) {
                    if (t.IsFunctional) {
                        if (!t.Enabled) t.Enabled = true;
                        if (met < aForce) {
                            t.ThrustOverridePercentage = 1;
                            applied += met;
                            aForce -= met;
                        } else {
                            var p = aForce / met;
                            t.ThrustOverridePercentage = (float)p;
                            applied += t.ThrustOverride;
                            aForce -= t.ThrustOverride;
                            //applied += met * p;
                        }
                    }
                } else {
                    if (t.Enabled) t.Enabled = false;
                    t.ThrustOverride = 0;
                }
            }
            forces[aList] = forceSum;
            return applied;
        }
    }
}
