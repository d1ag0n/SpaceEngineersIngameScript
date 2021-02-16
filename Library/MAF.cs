using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    static class MAF
    {
        public static double angleBetween(Vector3D a, Vector3D b) {
            double result = 0;
            if (!Vector3D.IsZero(a) && !Vector3D.IsZero(b))
                result = Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));

            return result;
        }
        public static double angleBetween(Vector3D a, Vector3D b, out double dot) {
            double result = 0;
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                dot = 0;
            else {
                dot = a.Dot(b);
                result = Math.Acos(MathHelper.Clamp(dot / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }
            return result;
        }
        public static Vector3D project(Vector3D a, Vector3D b) => a.Dot(b) / b.LengthSquared() * b;
        public static Vector3D project(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return aDot / b.LengthSquared() * b;
        }
    }
}
