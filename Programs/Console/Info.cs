using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace commandline {
    class Info {
        static void collide() {
            var thing = new Vector3D(0, 0, -10);
            var me = new Vector3D(0, 0, -1);
            var disp = thing - me;
            var norm = disp;
            var mag = norm.Normalize();

            var velo = new Vector3D(1, 0, -3);
            var veloNorm = velo;
            var veloMag = veloNorm.Normalize();

            double d1, d2;
            var r1 = reject(veloNorm, norm, out d1);
            var r2 = reject(norm, veloNorm, out d2);

            return;
        }
        public static Vector3D reject(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return a - aDot / b.Dot(b) * b;
        }
        public static Vector3D reject(Vector3D a, Vector3D b) => a - a.Dot(b) / b.Dot(b) * b;
        static void accel() {
            // available thrust - left up front
            var thrustLUF = new Vector3D(1, 2, 3);

            // available thrust - right down back
            var thrustRDB = new Vector3D(4, 5, 6);

            // random direction
            var r = new Random();
            var dir = Vector3D.Normalize(new Vector3D(r.NextDouble() * 2 - 1, r.NextDouble() * 2 - 1, r.NextDouble() * 2 - 1));

            // extract absolute max values from direction
            double dirMax = 0;
            absMax(dir.X, ref dirMax);
            absMax(dir.Y, ref dirMax);
            absMax(dir.Z, ref dirMax);

            // calculate directional percentages
            var accelPercent = new Vector3D(dir.X / dirMax, dir.Y / dirMax, dir.Z / dirMax);

            // extract thruster forces
            var thrustXYZ = new Vector3D(
                dir.X < 0 ? thrustLUF.X : thrustRDB.X, // left/right
                dir.Y < 0 ? thrustLUF.Y : thrustRDB.Y, // up/down
                dir.Z < 0 ? thrustLUF.Z : thrustRDB.Z  // front/back
            );

            // extract max values from thrusters
            double thrustMax = 0;
            max(thrustXYZ.X, ref thrustMax);
            max(thrustXYZ.Y, ref thrustMax);
            max(thrustXYZ.Z, ref thrustMax);

            // calculate thrust percentages
            var thrustPercent = new Vector3D(thrustXYZ.X / thrustMax, thrustXYZ.Y / thrustMax, thrustXYZ.Z / thrustMax);

            var accelMax = new Vector3D();

            var ldot = dir.Dot(Vector3D.Left);
            var udot = dir.Dot(Vector3D.Up);
            var fdot = dir.Dot(Vector3D.Forward);

            var maxForce = new Vector3D(100, 100, 100);
            var maxForceMag = maxForce.Length();
            var force = new Vector3D(100 * ldot, 100 * udot, 100 * fdot);
            var forceMag = force.Length();
            var accel = dir * forceMag;
            var accelMag = accel.Length();
            // accelMag == forceMag

            return;
        }
        public static void max(double a, ref double b) {
            if (a > b) {
                b = a;
            }
        }
        public static void absMax(double a, ref double b) {
            a = Math.Abs(a);
            if (a > b) {
                b = a;
            }
        }
        static void Main(string[] args) {
            //accel();
            collide();
            Console.ReadKey();
        }
    }
}
