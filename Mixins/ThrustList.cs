using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {

    public class ThrustList : BlockDirList<IMyThrust> {
        public double LeftForce => forces[2];
        public double RightForce => forces[3];
        public double UpForce => forces[4];
        public double DownForce => forces[5];
        public double FrontForce => forces[0];
        public double BackForce => forces[1];
        double[] forces = new double[6];
        readonly ThrustModule mThrust;

        public ThrustList(ThrustModule aThrust) {
            mThrust = aThrust;
        }

        // todo index based acceleration modification, track how many thrusters currently powered to full

        // Forward = 0 provides backward acceleration
        // Backward = 1 provides forward acceleration
        // Left = 2 provides right acceleration
        // Right = 3 provides left acceleration
        // Up = 4 provides down acceleration
        // Down = 5 provides up acceleration
        public void Update(Vector3D aAccel, double aMass, bool emergency = false) {
            Vector3D original = aAccel;
            //ModuleManager.logger.log("original", original);
            int f = 0, b = 1, l = 2, r = 3, u = 4, d = 5;
            if (aAccel.Z < 0) {
                f = 1;
                b = 0;
            }
            if (aAccel.X < 0) {
                r = 2;
                l = 3;
            }
            if (aAccel.Y > 0) {
                u = 5;
                d = 4;
            }

            // converting to force here
            var z = Math.Abs(aAccel.Z) * aMass;
            var x = Math.Abs(aAccel.X) * aMass;
            var y = Math.Abs(aAccel.Y) * aMass;

            if (emergency) {
                mThrust.mLog.log("EMERGENCY!");
            } else {
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

            var applied = new Vector3D(handleLists(x, l, r), handleLists(y, u, d), handleLists(z, f, b));
            if (aAccel.Z < 0) {
                applied.Z *= -1.0;
            }
            if (aAccel.X < 0) {
                applied.X *= -1.0;
            }
            if (aAccel.Y < 0) {
                applied.Y *= -1.0;
            }
            
        }
        Vector3D[] mCoT = new Vector3D[6];
        public void calculateCoT() {
            for (int i = 0; i < 6; i++) {
                Vector3D weightedCoT = Vector3D.Zero;
                double totalForce = 0;
                var list = mLists[i];
                foreach (var t in mLists[i]) {
                    var force = t.MaxEffectiveThrust;
                    weightedCoT += force * t.WorldMatrix.Translation;
                    totalForce += force;
                    if (i == 1) {
                        mThrust.mLog.log(mThrust.mLog.gps($"T{(Base6Directions.Direction)i}", t.WorldMatrix.Translation));
                    }
                }
                mCoT[i] = weightedCoT / totalForce;
                
                //mThrust.mLog.log(mThrust.mLog.gps($"CoT{(Base6Directions.Direction)i}", mCoT[i]));
            }

        }
        public void AllStop() {
            foreach (var list in mLists) {
                foreach (var t in list) {
                    t.ThrustOverridePercentage = 0f;
                    t.Enabled = false;
                }
            }
        }
        // Forward = 0 provides backward acceleration
        // Backward = 1 provides forward acceleration
        // Left = 2 provides right acceleration
        // Right = 3 provides left acceleration
        // Up = 4 provides down acceleration
        // Down = 5 provides up acceleration
        public Vector3D MaxAccel(Vector3D aLocalVec, double aMass) {
            int f = 1, l = 3, u = 4;
            if (aLocalVec.Z < 0) {
                f = 0;
            }
            if (aLocalVec.X < 0) {
                l = 2;
            }
            if (aLocalVec.Y < 0) {
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

        double handleLists(double aForce, int aUse, int aDisable) {
            var forceSum = 0.0;
            foreach (var t in mLists[aDisable]) {
                t.Enabled = false;
                forceSum += t.MaxEffectiveThrust;
            }
            forces[aDisable] = forceSum;

            return runList(aForce, aUse);
        }

        double runList(double aForce, int aList) {
            double applied = 0;
            var forceSum = 0.0;
            foreach (var t in mLists[aList]) {
                double met = t.MaxEffectiveThrust;
                
                forceSum += met;
                if (aForce > 0) {
                    if (t.IsFunctional) {
                        t.Enabled = true;
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
                    t.Enabled = false;
                }
            }
            forces[aList] = forceSum;
            return applied;
        }
    }
}
