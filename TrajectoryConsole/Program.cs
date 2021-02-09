using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace TrajectoryConsole
{
    class Program
    {
        static readonly Program p;
        static Program() { 
            p = new Program();
            Console.WriteLine($"Right     {Vector3D.Right}");   // Right     X:1 Y:0 Z:0            
            Console.WriteLine($"Up        {Vector3D.Up}");      // Up        X:0 Y:1 Z:0
            Console.WriteLine($"Forward   {Vector3D.Forward}"); // Forward   X:0 Y:0 Z:-1

            p.main();
            Console.ReadKey();
        }
        static void Main(string[] args) => p.main();
        const double deg = Math.PI / 180.0;
        double rad2deg(double rad) => rad / deg;
        double deg2rad(double rad) => rad * deg;
        void main() {
            Console.WriteLine($"180 * deg = {180 * deg}");
            Console.WriteLine($"180 deg2rad = {deg2rad(180)}");
            Console.WriteLine($"PI rad2deg = {rad2deg(Math.PI)}");
            Console.WriteLine($"deg = {deg}");
            var maxLean = Math.PI / 8;
            //maxLean = 999; // subvert lean constraint
            Console.WriteLine($"maxLean = {maxLean / deg}°");
            var velocity = new Vector3D(3, -1.3, 9);
            var gravity = new Vector3D(0, -2.452, 0);
            var gravityDir = Vector3D.Normalize(gravity);
            var v2g = project(velocity, gravityDir);

            Console.WriteLine($"v2g = {v2g}");

            
            var thrust = new Vector3D(10, -10, 0);
            var thrustDir = Vector3D.Normalize(thrust);
            Console.WriteLine($"gravity = {gravity}");
            var target = new Vector3D(100, 90, 0); // target should be right and up/down from CoM make y +/- respectivly
            var targetDirection = Vector3D.Normalize(target);
            //var targetProjection = new Vector3D(100, 0, 0);
            double elevation;
            var targetProjection = project(target, Vector3D.Zero, -gravity, out elevation);
            Console.WriteLine($"elevation = {elevation}");
            var targetProjectionDirection = Vector3D.Normalize(targetProjection);
            var targetDot = targetProjectionDirection.Dot(targetDirection);
            var angle = Math.Acos(targetDot); // angle is the virtual plane angle
            if (angle > maxLean) {
                Console.WriteLine($"ANGLE CONSTRAINED {angle / deg}°");
                angle = maxLean;
            }
            var gravDot = gravity.Dot(targetDirection);
            if (gravDot > 0) {
                // target below
                Console.WriteLine("target below");
                angle = -angle;
            } else {
                // target above
                Console.WriteLine("target above");
            }

            // todo constrain angle to maxLean

            Console.WriteLine($"grav dot target = {gravDot}");
            Console.WriteLine($"target angle = {angle / deg}°");

            // create normal for the trajectory plane
            var axis = Vector3D.Backward; // little sideways since this is imagined in a 2d perspective, want to turn "up" into lean towards target
            var mat = MatrixD.CreateFromAxisAngle(axis, angle);
            var lean = Vector3D.TransformNormal(Vector3D.Up, mat); // lean is the trajectory plane normal
            Console.WriteLine($"lean = {lean}");
            Console.WriteLine($"||lean|| = {lean.Length()}");

            var leanDot = Vector3D.Up.Dot(lean);
            var leanAngle = Math.Acos(leanDot);
            Console.WriteLine($"leanAngle = {leanAngle / deg}°"); // this is verification of the transformation

            // calculate distance from plane
            double comDot; // distance from trajectory plane - below + above
            var comProjection = project(Vector3D.Zero, target, lean, out comDot);
            Console.WriteLine($"com projection = {comProjection}");
            Console.WriteLine($"comDot = {comDot}");
            var comProjectionMag = comProjection.Length();
            var comStr = comProjectionMag > 0.001 ? comProjectionMag.ToString() : "~0";
            Console.WriteLine($"||com projection|| = {comStr}"); // com projection mag = abs(comDot)

            //var force = forceAtLean(angle, 1 / deg, 1, 1);
            var force = forceAtLean(90 * deg, 45 * deg, gravity.Length(), 1);
            Console.WriteLine($"force = {force}");

        }
        Vector3D reject(Vector3D a, Vector3D b) {
            return a - (a.Dot(b) / b.Dot(b)) * b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a">any vector</param>
        /// <param name="b">any unit vector</param>
        /// <returns></returns>
        Vector3D project(Vector3D a, Vector3D b) {
            return a.Dot(b) * b;
        }
        Vector3D project(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal, out double aDot) {
            aDot = Vector3D.Dot(aTarget - aPlane, aNormal);
            return aTarget - (aDot * aNormal);
        }
        /// <summary>
        /// returns force needed to maintain a trajectory on a virtual plane
        /// </summary>
        /// <param name="B">virtual plane angle</param>
        /// <param name="C">lean angle</param>
        double forceAtLean(double B, double C, double gravity, double mass) {
            //
            //           C
            //          / \
            //         /   \
            //        /     \
            //       /       \
            //      b         a
            //     /           \
            //    /             \
            //   /               \
            //  A-------c---------B
            //          
            // A nothing
            // B virtual plane angle
            // C thrust deviation from gravity
            // a gravity * mass
            // b desired thrust
            // c nothing
            //var B = Math.Acos(mvDirection2Objective.Dot(mvGravityDirection));
            //var C = Math.Acos(mvGravityDirection.Dot(mRC.WorldMatrix.Down));
            var A = Math.PI - B - C;
            var b = (gravity * mass) * (Math.Sin(B) / Math.Sin(A));
            return b;
        }
    }
}
