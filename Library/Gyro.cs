using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Gyro {
        const double updatesPerSecond = 6.0;
        double proportionalConstant = 2.5;
        double integralConstant = 0.1;
        double derivativeConstant = 1.0;
        double pidLimit = 10;
        const double timeLimit = 1.0 / updatesPerSecond;

        PID pidPitch;
        PID pidRoll;

        readonly GTS gts;
        readonly Logger g;
        readonly List<IMyGyro> mGyros = new List<IMyGyro>();
        readonly IMyShipController mRC;
        public Gyro(GTS aGTS, Logger aLogger) {
            gts = aGTS;
            g = aLogger;
            gts.initList(mGyros);
            gts.get(ref mRC);
            initPid();
            manual(false);
        }
        void initPid() {
            pidPitch = new PID(proportionalConstant, integralConstant, derivativeConstant, 0.25, timeLimit);
            pidRoll = new PID(proportionalConstant, integralConstant, derivativeConstant, 0.25, timeLimit);
        }
        public void setPid(double p, double i, double d) {
            proportionalConstant = p;
            integralConstant = i;
            derivativeConstant = d;
            initPid();
        }

        public void Rotate(Vector3D aDesiredDown) {
            double pitch, roll;
            Vector3D dir;

            var sv = mRC.GetShipVelocities();
            var av = MAF.world2dir(sv.AngularVelocity, mRC.WorldMatrix);
            //var av = sv.AngularVelocity;


            if (aDesiredDown.IsZero()) {
                applyGyroOverride(0, 0, 0);
                return;
            }



            //var roughDesiredDirection = Base6Directions.GetClosestDirection(aDesiredDown);
            //g.log("my down from ", roughDesiredDirection, "-ish");
            //g.log(MAF.angleBetween(mRC.WorldMatrix.Down, aDesiredDown));

            //g.log("angular velocity", av);
            //var rps = av.Length();
            //g.log("angular rps ", rps);
            //var rpm = rps * MathHelper.RadiansPerSecondToRPM;
            //g.log("angular rpm ", rpm);

            // local angular velocity
            // +X = +pitch
            // -X = -pitch
            // +Y = -yaw
            // -Y = +yaw
            // +Z = -roll
            // -Z = +roll

            //g.log("dd", aDesiredDown);
            getRotationAnglesFromDown(aDesiredDown, out pitch, out roll);
            //g.log("pitch ", pitch);
            //g.log("roll  ", roll);
            //applyGyroOverride(pitch, 0.0, roll);
            //return;
            //g.log("pitch ", pitch);
            //g.log("roll  ", roll);

            // av = angular velocity
            // if we ask for 10 lastPitch is 10
            // av check pitch = 5
            // pitchDif = 10 - 5 = 5
            // if we ask for 1 lastPitch is 1
            // av check pitch = 5
            // pitchDif = 1 - 5 = -4
            // pitchDif = lastPitch - pitchCheck
            // 

            var min = 0.1;
            var pitchDif = pitch - av.X;
            //g.log("pitchDif ", pitchDif);
            if (Math.Abs(pitchDif) < min)
                pitchDif = 0;

            var rollDif = roll + av.Z;
            //g.log("rollDif  ", rollDif);
            if (Math.Abs(rollDif) < min)
                rollDif = 0;

            var fact = 6.0;
            applyGyroOverride(pitch + pitchDif * fact, av.Y, roll + rollDif * fact);
            //lastPitch = pitch;
            //lastRoll = roll;
            //applyGyroOverride(0.0, 0.0, 0.5);

        }

        public void setGyrosEnabled(bool aValue) {
            foreach (var g in mGyros)
                g.Enabled = aValue;
        }

        bool lastManual = true;
        void manual(bool enabled) {
            if (lastManual != enabled) {
                lastManual = enabled;
                foreach (var gy in mGyros) {
                    if (gy.GyroOverride) {
                        gy.Yaw = 0;
                        gy.Pitch = 0;
                        gy.Roll = 0;
                        gy.GyroOverride = !enabled;
                    } else {
                        gy.GyroOverride = !enabled;
                        gy.Yaw = 0;
                        gy.Pitch = 0;
                        gy.Roll = 0;
                    }
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

            foreach (var gy in mGyros) {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gy.WorldMatrix));
                //g.log("transformedRotationVec", transformedRotationVec);

                gy.Pitch = (float)transformedRotationVec.X;
                gy.Yaw = (float)transformedRotationVec.Y;
                gy.Roll = (float)transformedRotationVec.Z;

                //g.log("gyro", transformedRotationVec);

            }
        }
        /*
        Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        modified by d1ag0n for pitch and roll
        */
        void getRotationAnglesFromDown(Vector3D targetVector, out double pitch, out double roll) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(mRC.WorldMatrix));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Down, flattenedTargetVector);
            if (localTargetVector.Z > 0)
                pitch = -pitch;

            roll = MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X > 0)
                roll = -roll;
        }
    }
}
