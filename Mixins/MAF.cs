﻿using System;
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
    }
}
