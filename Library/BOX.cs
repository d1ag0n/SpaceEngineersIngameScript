using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// kbox = 1000m^3 
    /// cbox = 100m^3 and has an index position within the k box from 0 to 999 
    /// kbox exists in the world
    /// cbox exists in the kbox
    /// </summary>
    static class BOX
    {
        static readonly Vector3D[] points = new Vector3D[2];
        const int cmax = 10;
        public const double cdist = 173.205080756888;
        /// <summary>
        /// Returns the bounding K box in world coordinates
        /// </summary>
        /// <param name="aWorldPosition"></param>
        /// <returns></returns>
        public static BoundingBoxD GetKBox(Vector3D aWorldPosition) {
            var k = WorldToK(aWorldPosition);
            var v = KToWorld(k);
            aWorldPosition = new Vector3D(1000);
            if (v.X < 0)
                aWorldPosition.X = -1000;
            if (v.Y < 0)
                aWorldPosition.Y = -1000;
            if (v.Z < 0)
                aWorldPosition.Z = -1000;
            points[0] = v;
            points[1] = v + aWorldPosition;
            return BoundingBoxD.CreateFromPoints(points);
        }
        /// <summary>
        /// Returns the bounding C box in world coordinates
        /// </summary>
        /// <param name="aWorldPosition"></param>
        /// <returns></returns>
        public static BoundingBoxD GetCBox(Vector3D aWorldPosition) {
            var k = KToWorld(WorldToK(aWorldPosition));
            var v2cresult = KToC(aWorldPosition - k);
            var c = k + CToK(v2cresult);
            k = new Vector3D(100);
            if (c.X < 0)
                k.X = -100;
            if (c.Y < 0)
                k.Y = -100;
            if (c.Z < 0)
                k.Z = -100;
            points[0] = c;
            points[1] = c + k;
            return BoundingBoxD.CreateFromPoints(points);
        }
        /// <summary>
        /// Returns the index of the cbox for the given cbox position
        /// </summary>
        /// <param name="v">value from </param>
        /// <returns></returns>
        public static int CVectorToIndex(Vector3D v) => ((int)v.Z * cmax * cmax) + ((int)v.Y * cmax) + (int)v.X;
        public static Vector3D CIndexToVector(int idx) {
            int z = idx / (cmax * cmax);
            idx -= (z * cmax * cmax);
            int y = idx / cmax;
            int x = idx % cmax;
            return new Vector3D(x, y, z);
        }
        /// <summary>
        /// k resolution boxing
        /// </summary>
        /// <param name="v">World position</param>
        /// <returns></returns>
        public static Vector3D WorldToK(Vector3D v) => v2n(v, 1000);
        /// <summary>
        /// k resolution unboxing
        /// </summary>
        /// <param name="v">KBox position</param>
        /// <returns></returns>
        public static Vector3D KToWorld(Vector3D v) => n2v(v, 1000);
        /// <summary>
        /// Returns the world position that is the same relative position in the adjacent cbox 
        /// that is closest to the provided direction, uses base6 directions only
        /// intended to receive a cbox center as the aWorldPosition but this is not necessary
        /// </summary>
        /// <param name="aWorldPosition">position in the world</param>
        /// <param name="aWorldDirection">unit vector direction in world space</param>
        /// <returns></returns>
        public static Vector3D MoveC(Vector3D aWorldPosition, Vector3D aWorldDirection) => MoveN(aWorldPosition, aWorldDirection, 100.0);
        /// <summary>
        /// Returns the world position that is the same relative position in the adjacent kbox 
        /// that is closest to the provided direction, uses base6 directions only
        /// intended to receive a kbox center as the aWorldPosition but this is not necessary
        /// </summary>
        /// <param name="aWorldPosition">position in the world</param>
        /// <param name="aWorldDirection">unit vector direction in world space</param>
        /// <returns></returns>
        public static Vector3D MoveK(Vector3D aWorldPosition, Vector3D aWorldDirection) => MoveN(aWorldPosition, aWorldDirection, 1000.0);
        public static BoundingBoxD moveTowardsDir(BoundingBoxD aBox, Vector3D aDir) {
            var disp = aBox.Max - aBox.Min;
            var mag = disp.Length();
            points[0] = aBox.Min + (aDir * mag);
            points[1] = aBox.Max + (aDir * mag);
            return BoundingBoxD.CreateFromPoints(points);
        }
        public static BoundingBoxD moveTowardsPos(BoundingBoxD aBox, Vector3D aTarget) {
            var disp = aBox.Max - aBox.Min;
            var mag = disp.Length() * 0.75;
            var dir = Vector3D.Normalize(aTarget - aBox.Center);
            points[0] = aBox.Min + (dir * mag);
            points[1] = aBox.Max + (dir * mag);
            return BoundingBoxD.CreateFromPoints(points);
        }
        static Vector3D MoveN(Vector3D aWorldPosition, Vector3D aWorldDirection, double n) {
            int d = (int)Base6Directions.GetClosestDirection((Vector3)aWorldDirection);
            Vector3D dir = Base6Directions.Directions[d];
            aWorldPosition.X += dir.X * n;
            aWorldPosition.Y += dir.Y * n;
            aWorldPosition.Z += dir.Z * n;
            return aWorldPosition;
        }
        /// <summary>
        /// C resolution boxing, previous v2c
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static Vector3D KToC(Vector3D v) => v2n(v, 100);
        /// <summary>
        /// C resolution unboxing, previous c2v
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static Vector3D CToK(Vector3D v) => n2v(v, 100);
        /// <summary>
        /// variable resolution unboxing
        /// </summary>
        /// <param name="v">Position</param>
        /// <param name="n">resolution</param>
        /// <returns></returns>
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
        /// <summary>
        /// variable resolution boxing
        /// </summary>
        /// <param name="v">Position</param>
        /// <param name="n">Resolution</param>
        /// <returns></returns>
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
    }
}
