using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace commandline
{
    class thrustvector
    {
        public static void calculateThrustVector(Vector3D aTarget, Vector3D aCoM, Vector3D aGravity, Vector3D aThrustVector, double aMass, double aNewtons) {
            // get gravity direction
            var gravityDirection = aGravity;
            var gravityMagnitude = gravityDirection.Normalize();

            // project the target onto the plane defined by centerOfMass and gravity normal
            var targetProjection = project(aTarget, aCoM, gravityDirection);

            // get the angle between the target and it's projected position
            var dir2target = Vector3D.Normalize(aTarget - aCoM);
            var dir2targetProjection = Vector3D.Normalize(targetProjection - aCoM);
            var angle = angleBetween(dir2targetProjection, dir2target);

            var dot = gravityDirection.Dot(Vector3D.Normalize(aTarget - aCoM));
            // - is above here
            if (dot > 0) {
                angle = -angle;
            }
            var axis = gravityDirection.Cross(dir2target);
            var transform = Matrix.CreateFromAxisAngle(axis, (float)angle);

            var leanMax = maxLean(gravityMagnitude, aMass, aNewtons, angle);

            var t = thrustAtAngle(aGravity, gravityDirection, angle, aThrustVector, aMass);
            var gravityTransformed = Vector3D.TransformNormal(gravityDirection, transform) * gravityMagnitude;
            
            var tv = vectorThrustAtAngle(gravityTransformed, aThrustVector, aMass);
            Console.WriteLine($"thrustAtAngle                 = {t}");            
            Console.WriteLine($"vectorThrustAtAngle magnitude = {tv.Length()}");
            Console.WriteLine($"gravityTransformed");
            Console.WriteLine(gravityTransformed);
        }
        static Vector3D vectorThrustAtAngle(Vector3D aGravity, Vector3D aThrustDirection, double aMass) => (aGravity * aMass).Dot(aThrustDirection) * aThrustDirection;

        static double maxLean(double aGravity, double aMass, double aNewtons, double aAngle) => Math.PI - (Math.PI / 2.0 + aAngle) - Math.Asin((aGravity * aMass) * Math.Sin(Math.PI / 2.0) / aNewtons);

        static Vector3D project(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) => aTarget - (Vector3D.Dot(aTarget - aPlane, aNormal) * aNormal);

        static double thrustAtAngle(Vector3D aGravity, Vector3D aGravityDirection, double aAngle, Vector3D aThrustVector, double aMass) {
            var B = Math.PI / 2.0 + aAngle; // gravity plane
            var C = angleBetween(aGravityDirection, aThrustVector);
            var G = aGravity * aMass;
            var A = Math.PI - B - C; // remaining angle
            var b = G * Math.Sin(B) / Math.Sin(A);// length of thrust vector to gravity plane
            var t = b.Length();
            return t;
        }
        static void foo() {
            var p = new PlaneD();
            
        }

        static double angleBetween(Vector3D a, Vector3D b) {
            var dot = a.Dot(b);
            if (dot < -1.0) {
                dot = -1.0;
            } else if (dot > 1.0) {
                dot = 1.0;
            }
            var result = Math.Acos(dot);
            //log("angleBetween ", result);
            return result;
        }
    }
}
