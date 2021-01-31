using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class box
    {
        public const double cdist = 173.205080756888;
        public static BoundingBoxD k(Vector3D aWorldPosition) {
            var k = v2k(aWorldPosition);
            var v = k2v(k);
            aWorldPosition = new Vector3D(1000);
            if (v.X < 0)
                aWorldPosition.X = -1000;
            if (v.Y < 0)
                aWorldPosition.Y = -1000;
            if (v.Z < 0)
                aWorldPosition.Z = -1000;
            return new BoundingBoxD(v, v + aWorldPosition);
        }
        public static BoundingBoxD c(Vector3D aWorldPosition) {
            var k = k2v(v2k(aWorldPosition));
            var c = k + c2v(v2c(aWorldPosition - k));
            k = new Vector3D(100);
            if (c.X < 0)
                k.X = -100;
            if (c.Y < 0)
                k.Y = -100;
            if (c.Z < 0)
                k.Z = -100;
            
            return new BoundingBoxD(c, c + k);
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
        static Vector3D v2k(Vector3D v) => v2n(v, 1000);
        static Vector3D k2v(Vector3D v) => n2v(v, 1000);
        const int cmax = 10;
        public static int toIndex(Vector3D v) => ((int)v.Z * cmax * cmax) + ((int)v.Y * cmax) + (int)v.X;
        public static Vector3D toVector(int idx) {
            int z = idx / (cmax * cmax);
            idx -= (z * cmax * cmax);
            int y = idx / cmax;
            int x = idx % cmax;
            return new Vector3D(x, y, z);
        }
    }
}
