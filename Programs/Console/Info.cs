using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using IngameScript;

namespace commandline {
    class Info {
        static void Main() => round();

        public static void round() {
            var v = 15L;

            List<long> lst = new List<long>();
            for (long i = -100L; i < 100L; i++) {
                lst.Add(round(i + (i > 0L ? 5L : -5L)));
            }
            var result = round(v); ;
        }
        public static long round(long value, long interval = 10L) => (value / interval) * interval;
        public static int PageCount(int itemCount) => (itemCount / 6) + 1;
        public static int PageNumber(int pageNumber, int itemCount) => Math.Abs(pageNumber % PageCount(itemCount));
        static void sphere() {

            var itemCount = 11;
            var pageCount = PageCount(itemCount);
            
            List<int> lst = new List<int>();

            for (int i = 0; i < 26; i++) {
               
            }
            var bs1 = new BoundingSphereD(new Vector3D(1d, 1d, 1d), 1d);
            var bs2 = new BoundingSphereD(new Vector3D(0d, 0d, 0d), 1d);

            
            
            return;
        }
        static void vecIt() {

            int g = 0;
            int h = 1;
            int v = h / g;
            var start = new Vector3I(-3, -3, -3);
            var end = new Vector3I(3, 3, 3);
            var vi = new Vector3I_RangeIterator(ref start, ref end);
            while (vi.IsValid()) {

                Console.WriteLine(vi.Current);
                vi.MoveNext();
            }
            Console.WriteLine("Done");
            Console.ReadKey();
        }
        static void perp() {
            var v1 = Vector3D.Up;
            return;
        }
        static void accelCalcs() {
            /*
             *  F = 4320000
             *  m = 5406435
             *  1.664 for 1
             *  3.328 for 2
             *  4.992 for 3
             *  6.656 for 4
             *  8.320 for 5
             *  9.985 for 6
             *  
             *  (V * V) / (2.0 * a)
             *  1908.291856 / 6.656
             *  
             *  a = 9.589
             */

            var a = 1.664;
            var m = 5406435;
            var F = 4320000 * 12;
            var v = 99.88;

            var stp = stop(v, m, F);
            return;
        }
        static double stop(double v, double m, double F) => (v * v) * m / (2 * F);
        static void vecscale() {
            var v = new Vector3D(1, 10, 100);
            var n = Vector3D.Normalize(v);
            var p = v * 0.1;
            var pn = Vector3D.Normalize(p);
            var ab = MAF.angleBetween(n, pn);
            MaxAccel(MAF.ranDir(), 1000);
            return;
        }
        const double force = 1000;
        public static Vector3D MaxAccel(Vector3D aDirection, double aMass) {

            var amp = aDirection * 1000.0;


            var z = Math.Abs(amp.Z) * aMass;
            var x = Math.Abs(amp.X) * aMass;
            var y = Math.Abs(amp.Y) * aMass;


            double ratio = force / z;
            double tempRatio = force / x;

            if (tempRatio < ratio) {
                ratio = tempRatio;
            }
            tempRatio = force / y;
            if (tempRatio < ratio) {
                ratio = tempRatio;
            }
            var result = new Vector3D(x, y, z);
            if (ratio < 1.0) {
                result *= ratio;
                z *= ratio;
                x *= ratio;
                y *= ratio;
            }

            if (aDirection.Z < 0) {
                result.Z *= -1.0;
            }
            if (aDirection.X < 0) {
                result.X *= -1.0;
            }
            if (aDirection.Y < 0) {
                result.Y *= -1.0;
            }

            var norm = Vector3D.Normalize(result);
            var ab = MAF.angleBetween(aDirection, norm);
            return result;
        }
        static void pass() {
            Vector3D destination = new Vector3D(2, 0, 10);
            Vector3D ship = new Vector3D(1, 0, 5);
            Vector3D obstacle = new Vector3D(-1, 0, 5);

            Vector3D shipToDest = destination - ship;
            Vector3D shipToObst = obstacle - ship;
            double dot = shipToDest.Dot(shipToObst);
            return;
        }
        static void collide() {
           
            bool value = 0 == new double?(0);


            var thing = new Vector3D(0, 20, 0);
            var me = new Vector3D(0, 10, 0);
            var disp = thing - me;
            var norm = disp;
            norm.Normalize();

            var velo = new Vector3D(1, 3, 0);
            var veloNorm = velo;   
            veloNorm.Normalize(); 

            var rejection = reject(veloNorm, norm);
            var intersection = thing + rejection;
            var bb = new BoundingBoxD();
            var rd = new RayD();
            
            return;
        }
        public static Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - ((aTarget - aPlane).Dot(aNormal) * aNormal);
        public static Vector3D reject(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return a - aDot / b.LengthSquared() * b;
        }
        public static Vector3D wreject(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return a - aDot / b.LengthSquared() * b;
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

    }
}
