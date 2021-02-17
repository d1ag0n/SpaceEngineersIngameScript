using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    static class MAF
    {
        static readonly DateTime epoch = new DateTime(2020, 1, 1);
        public static double time => (DateTime.Now - epoch).TotalSeconds;
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
        // todo use reject?
        // orthogonal projection is vector rejection
        public static Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - ((aTarget - aPlane).Dot(aNormal) * aNormal);

        // todo use reject?
        public static Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal, out double aDot) {
            aDot = (aTarget - aPlane).Dot(aNormal);
            return aTarget - (aDot * aNormal);
        }

        public static Vector3D reject(Vector3D a, Vector3D b) => a - a.Dot(b) / b.Dot(b) * b;
        public static Vector3D reject(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return a - aDot / b.Dot(b) * b;
        }
    }
}
