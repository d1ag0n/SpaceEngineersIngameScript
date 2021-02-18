using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Gyro
    {
        readonly GTS gts;
        readonly Logger g;
        readonly List<IMyGyro> mGyros = new List<IMyGyro>();
        readonly IMyShipController mRC;
        public Gyro(GTS aGTS, Logger aLogger) {
            gts = aGTS;
            g = aLogger;
            gts.initList(mGyros);
            gts.get(ref mRC);
            manual(true);
        }
        /*
         * 
         * 
         * 
         * 
         */ 
        public void Rotate(Vector3D aDesiredDown) {
            double pitch, roll;
            Vector3D dir;

            var sv = mRC.GetShipVelocities();
            var av = sv.AngularVelocity;

            var man = false;
            if (aDesiredDown.IsZero()) {
                man = true;
                aDesiredDown = Vector3D.Down;
            }

            manual(man);

            getRotationAnglesFromDown(aDesiredDown, out pitch, out roll);
            var roughDesiredDirection = Base6Directions.GetClosestDirection(aDesiredDown);
            g.log("my down from ", roughDesiredDirection, "-ish");
            g.log(MAF.angleBetween(mRC.WorldMatrix.Down, aDesiredDown));
            g.log("pitch ", pitch);
            g.log("roll  ", roll);

            var rps = av.Length();
            g.log("angular rps ", rps);
            var rpm = rps * MathHelper.RadiansPerSecondToRPM;
            g.log("angular rpm ", rpm);
            veloInfo(av);
            var roughAxis = Base6Directions.GetClosestDirection(av);
            g.log("rotation axis ", roughAxis, "-ish");

            if (!man) {
                var desiredRotationAxis = aDesiredDown.Cross(mRC.WorldMatrix.Down);
                var roughDesiredAxis = Base6Directions.GetClosestDirection(desiredRotationAxis);
                g.log("desired rotation axis ", roughDesiredAxis, "-ish");
            }

        }

        void veloInfo(Vector3D aVelo) {
            double pitch, roll;
            getRotationAngles(aVelo, out pitch, out roll);
            g.log("current pitch ", pitch, " roll ", roll);
        }
        
        bool lastManual = false;
        void manual(bool enabled) {
            if (lastManual != enabled) {
                lastManual = enabled;
                foreach (var gy in mGyros) {
                    gy.GyroOverride = !enabled;
                    gy.Yaw = 0;
                    gy.Pitch = 0;
                    gy.Roll = 0;
                }
            }
        }

        //Whip's ApplyGyroOverride Method v10 - 8/19/17
        void applyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed) {

            //pitch_speed = pidPitch.Control(pitch_speed);
            //roll_speed = pidRoll.Control(roll_speed);
            // Large gyro 3.36E+07
            // Small gyro 448000
            var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs             

            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, mRC.WorldMatrix);

            
            //g.log("ApplyGyroOverride");
            foreach (var gy in mGyros) {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gy.WorldMatrix));
                //g.log("transformedRotationVec", transformedRotationVec);
                

                gy.Pitch = (float)transformedRotationVec.X;
                gy.Yaw = (float)transformedRotationVec.Y;
                gy.Roll = (float)transformedRotationVec.Z;
            }
        }
        /*
        Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        modified by d1ag0n for pitch and roll
        */
        void getRotationAngles(Vector3D targetVector, out double pitch, out double roll) {

            var m = mRC.WorldMatrix;
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(m));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Forward, flattenedTargetVector);
            if (localTargetVector.Y < 0)
                pitch = -pitch;

            roll = MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X < 0)
                roll = -roll;
        }
        void getRotationAnglesFromDown(Vector3D targetVector, out double pitch, out double roll) {

            var m = mRC.WorldMatrix;
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(m));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Down, flattenedTargetVector);
            if (localTargetVector.Z > 0)
                pitch = -pitch;

            roll = MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X < 0)
                roll = -roll;
        }
    }
}
