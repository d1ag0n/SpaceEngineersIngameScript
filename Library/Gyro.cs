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
        readonly IMyRemoteControl mRC;
        public Gyro(GTS aGTS, Logger aLogger) {
            gts = aGTS;
            g = aLogger;
            gts.initList(mGyros);
            gts.get(ref mRC);
        }
        public void Update() {
            var sv = mRC.GetShipVelocities();
            g.log("angular", sv.AngularVelocity);
            
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
                gy.GyroOverride = true;

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
        void getRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double pitch, out double roll) {

            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Forward, flattenedTargetVector);
            if (localTargetVector.Y < 0)
                pitch = -pitch;

            roll = MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X < 0)
                roll = -roll;
        }
    }
}
