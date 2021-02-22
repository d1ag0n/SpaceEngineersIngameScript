using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class thrustvector
    {
        static void calculateThrustVector(Vector3D aTarget, Vector3D aCoM, Vector3D aGravity, Vector3D aThrustVector, double aMass, double aNewtons) {
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

            //var leanMax = maxLean(gravityMagnitude, aMass, aNewtons, angle);
            var t = thrustAtAngle(aGravity, gravityDirection, angle, aThrustVector, aMass);

        }

        static double maxLean(double aGravity, double aMass, double aNewtons) {
            var adjacent = aGravity * aMass;
            var hypotenuse = aNewtons;
            var cos = adjacent / hypotenuse;
            return Math.Acos(cos);

            //return Math.PI - (Math.PI / 2.0 + aAngle) - Math.Asin((aGravity * aMass) * Math.Sin(Math.PI / 2.0) / aNewtons);
        }

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
