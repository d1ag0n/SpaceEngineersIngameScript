using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    static class MAF
    {
        public static DateTime Epoch => DateTime.MinValue;
        public static DateTime Now => DateTime.Now;
        public static readonly Random random = new Random();
        public static Vector3D ranDir() => Vector3D.Normalize(new Vector3D(random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5));
        public static Vector3D ranBoxPos(BoundingBoxD aBox) =>
            new Vector3D(
                aBox.Min.X + random.NextDouble() * (aBox.Max.X - aBox.Min.X),
                aBox.Min.Y + random.NextDouble() * (aBox.Max.Y - aBox.Min.Y),
                aBox.Min.Z + random.NextDouble() * (aBox.Max.Z - aBox.Min.Z)
            );
        // probably Whiplash's code
        public static double angleBetween(Vector3D a, Vector3D b) {
            double result = 0;
            if (!Vector3D.IsZero(a) && !Vector3D.IsZero(b))
                result = Math.Acos(MathHelperD.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));

            return result;
        }
        // whiplash
        public static double angleBetween(Vector3D a, Vector3D b, out double dot) {
            double result = 0;
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                dot = 0;
            else {
                dot = a.Dot(b);
                result = Math.Acos(MathHelperD.Clamp(dot / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
            }
            return result;
        }
        public static Vector3D project(Vector3D a, Vector3D b) => a.Dot(b) / b.LengthSquared() * b;
        public static Vector3D project(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return aDot / b.LengthSquared() * b;
        }
        // orthogonal projection is vector rejection
        public static Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - ((aTarget - aPlane).Dot(aNormal) * aNormal);

        // todo use reject?
        public static Vector3D orthoProject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal, out double aDot) {
            aDot = (aTarget - aPlane).Dot(aNormal);
            return aTarget - (aDot * aNormal);
        }
        // len sq this.X * this.X + this.Y * this.Y + this.Z * this.Z
        public static Vector3D reject(Vector3D a, Vector3D b) => a - a.Dot(b) / b.LengthSquared() * b;
        public static Vector3D reject(Vector3D a, Vector3D b, out double aDot) {
            aDot = a.Dot(b);
            return a - aDot / b.LengthSquared() * b;    
        }


        // keen forums
        public static Vector3D local2pos(Vector3D position, MatrixD world) =>
            Vector3D.Transform(position, world);
        public static Vector3D local2dir(Vector3D direction, MatrixD world) =>
            Vector3D.TransformNormal(direction, world);
        public static Vector3D world2pos(Vector3D position, MatrixD world) =>
            Vector3D.TransformNormal(position - world.Translation, MatrixD.Transpose(world));
        public static Vector3D world2dir(Vector3D direction, MatrixD aWorld) =>
            Vector3D.TransformNormal(direction, MatrixD.Transpose(aWorld));

        // whiplash code modified by d1ag0n
        public static Vector2 getYawPitch(Vector3D targetVector, MatrixD aWorld) {
            var result = Vector2.Zero;
            
            var localTargetVector = world2dir(targetVector, aWorld);
            var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

            result.X = (float)MAF.angleBetween(Vector3D.Forward, flattenedTargetVector);
            if (localTargetVector.X < 0)
                result.X = -result.X;

            result.Y = (float)MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (targetVector.X > 0)
                result.Y = -result.Y;

            return result;
        }
        public static bool nearEqual(Vector3D a, Vector3D b, double epsilon = 0.000001) =>
            nearEqual((a - b).LengthSquared(), 0, epsilon);
        public static bool nearEqual(double a, double b, double epsilon = 0.000001) =>
            Math.Abs(a - b) < epsilon;
        /*
        /// Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        * /
        void GetRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double yaw, out double pitch) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

            yaw = AngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
            if (Math.Abs(yaw) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                yaw = Math.PI;

            if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                pitch = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
            else
                pitch = AngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive
        }//*/
        /*
        getRotationAnglesFromForward - modified by d1ag0n
        Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        */
        public static void getRotationAngles(Vector3D direction, MatrixD worldMatrix, out double yaw, out double pitch) {
            var localTargetVector = world2dir(direction, worldMatrix);
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = angleBetween(Vector3D.Forward, flattenedTargetVector);
            if (localTargetVector.Y < 0)
                pitch = -pitch;

            yaw = angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X < 0)
                yaw = -yaw;
        }
        // digi, whiplash - https://discord.com/channels/125011928711036928/216219467959500800/819309679863136257
        // var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);
        // var obb = new MyOrientedBoundingBoxD(bb, grid.WorldMatrix);
        public static MyOrientedBoundingBoxD obb(IMyCubeGrid aGrid, double aInflate = 0) {
            var bb = new BoundingBoxD(
                ((Vector3D)aGrid.Min - Vector3D.Half) * aGrid.GridSize,
                ((Vector3D)aGrid.Max + Vector3D.Half) * aGrid.GridSize
            );
            if (aInflate > 0) {
                bb = bb.Inflate(aInflate);
            }
            return new MyOrientedBoundingBoxD(bb, aGrid.WorldMatrix);
        }

        /*
        Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        modified by d1ag0n for pitch and roll
        */
        public static void getRotationAnglesFromDown(MatrixD world, Vector3D targetVector, out double pitch, out double roll) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(world));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = angleBetween(Vector3D.Down, flattenedTargetVector);
            if (localTargetVector.Z > 0)
                pitch = -pitch;

            roll = angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X > 0)
                roll = -roll;
        }
        public static void absMax(double a, ref double b) {
            a = Math.Abs(a);
            if (a > b) {
                b = a;
            }
        }

        public static long round(long value, long interval = 10L) => (value / interval) * interval;
    }
}
