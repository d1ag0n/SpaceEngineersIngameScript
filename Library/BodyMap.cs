using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class BodyMap
    {
        const double sqr2 = 1.4142135623731;
        enum MapState
        {
            start,  // goto v2c center, if terrain detected goto v2c that is up sqr2*100 meters
            goingDown   // ?same as start
        }
        static Vector3D v2n(Vector3D v, long n) {
            if (v.X < 0) {
                v.X -= n;
            }
            if (v.Y < 0) {
                v.Y -= n;
            }
            if (v.Z < 0) {
                v.Z -= n;
            }
            v.X = (long)v.X / n;
            v.Y = (long)v.Y / n;
            v.Z = (long)v.Z / n;
            return v;
        }
        static Vector3D n2v(Vector3D v, long n) {
            if (v.X < 0) {
                v.X++;
            }
            if (v.Y < 0) {
                v.Y++;
            }
            if (v.Z < 0) {
                v.Z++;
            }
            v.X *= n;
            v.Y *= n;
            v.Z *= n;
            return v;
        }
        public static Vector3D v2c(Vector3D v) => v2n(v, 100);
        public static Vector3D c2v(Vector3D v) => n2v(v, 100);
        public static Vector3D v2k(Vector3D v) => v2n(v, 1000);
        public static Vector3D k2v(Vector3D v) => n2v(v, 1000);
        public void reportBody() {
        }
    }
}
