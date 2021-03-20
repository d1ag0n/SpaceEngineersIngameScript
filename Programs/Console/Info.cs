using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace commandline {
    class Info {
        static void Main() => pass();
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
