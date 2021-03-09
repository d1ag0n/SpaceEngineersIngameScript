using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class GyroModule : Module<IMyGyro> {

        readonly Logger g;

        //*
        const double updatesPerSecond = 6.0;
        double proportionalConstant = 2;
        double integralConstant = 0;
        double derivativeConstant = 0.6;
        double pidLimit = 10;
        const double timeLimit = 1.0 / updatesPerSecond;
        PID pidPitch;
        PID pidRoll;
        void initPIDID() {
            
            pidPitch = new PID(proportionalConstant, integralConstant, derivativeConstant, 0.25, timeLimit);
            pidRoll = new PID(proportionalConstant, integralConstant, derivativeConstant, 0.25, timeLimit);
        }
        void initPID() {
            pidPitch = new PID(proportionalConstant, integralConstant, derivativeConstant, -10, 10, timeLimit);
            pidRoll = new PID(proportionalConstant, integralConstant, derivativeConstant, -10, 10, timeLimit);
        }
        public void setPid(double p, double i, double d) {
            proportionalConstant = p;
            integralConstant = i;
            derivativeConstant = d;
            initPID();
        }
        //*/
        

        ShipControllerModule _controller;
        ShipControllerModule controller {
            get {
                if (_controller == null) {
                    GetModule(out _controller);
                }
                return _controller;
            }
        }
        
        public GyroModule() {
            GetModule(out g);
            initPID();
        }
        
        public override bool Accept(IMyTerminalBlock b) {
            var result = base.Accept(b);
            if (result) {
                init(b as IMyGyro);
            }
            return result;
        }
        public void Rotate(Vector3D aDesiredDown) {
            if (aDesiredDown.IsZero() || controller == null) {
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



            //g.log("dd", aDesiredDown);

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

            double pitch = 0, yaw = 0, roll = 0;
            //getRotationAnglesFromDown(aDesiredDown, out pitch, out roll);
            getRotationAngles(aDesiredDown, controller.WorldMatrix, out yaw, out pitch);
            var smallMax = 0.4;
            var smallFact = 1.9;
            if (Math.Abs(pitch) < smallMax) {
                pitch *= smallFact;
            }

            if (Math.Abs(roll) < smallMax) {
                roll *= smallFact;
            }

            var sv = controller.GetShipVelocities();
            var av = MAF.world2dir(sv.AngularVelocity, controller.WorldMatrix);
            


            var pitchDif = (pitch - av.X);
            var rollDif = (roll + av.Z);

            var slowFact = 20.0;
            var fastFact = 2.0;
            var difMax = 0.09;
            if (Math.Abs(pitchDif) < difMax) {
                pitchDif = 0;
            } else {
                if (Math.Abs(pitch) < Math.Abs(av.X)) {
                    g.log("pitch too fast");
                    pitchDif *= slowFact;
                } else {
                    g.log("pitch too slow");
                    pitchDif *= fastFact;
                }
            }
            if (Math.Abs(rollDif) < difMax) {
                rollDif = 0;
            } else {
                if (Math.Abs(roll) < Math.Abs(av.Z)) {
                    rollDif *= slowFact;
                } else {
                    rollDif *= fastFact;
                }
            }

            g.log("desired pitch = ", pitch);
            g.log("desired roll  = ", roll);
            g.log();
            g.log("velo pitch    = ", av.X);
            g.log("velo roll     = ", -av.Z);
            g.log();
            g.log("diff pitch    = ", pitchDif);
            g.log("diff roll     = ", rollDif);
            g.log();
            // local angular velocity
            // +X = +pitch
            // -X = -pitch
            // +Y = -yaw
            // -Y = +yaw
            // +Z = -roll
            // -Z = +roll


            // want - at
            // at 1 want 2
            // 1 = 2 - 1

            // want - at
            // at 2 want 1
            // -1 = 1 - 2

            // want - at
            // at -1 want -2
            // -1 = -2 - -1





            //pitch += pitchDif * fact;
            //roll += rollDif * fact;

            // yaw = av.Y

            //pitch = (pitchDif / MathHelper.TwoPi) * controller.GyroSpeed;



            pitch += pitchDif;
            roll += rollDif;            
            
            applyGyroOverride(pitch , yaw, roll);
            
            //g.log("pitchDif ", pitchDif);
            //g.log("rollDif  ", rollDif);
            //lastPitch = pitch;
            //lastRoll = roll;
            //applyGyroOverride(0.0, 0.0, 0.5);

        }

        public void setGyrosEnabled(bool aValue) {
            foreach (var gy in Blocks) {
                gy.Enabled = aValue;
            }
        }
        static void init(IMyGyro aGyro) {

            if (!aGyro.Enabled) {
                aGyro.Enabled = true;
            }
            if (!aGyro.GyroOverride) {
                aGyro.GyroOverride = true; ;
            }
            aGyro.Yaw = 0;
            aGyro.Pitch = 0;
            aGyro.Roll = 0;
        }

        //Whip's ApplyGyroOverride Method v10 - 8/19/17
        void applyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed) {

            //pitch_speed = pidPitch.Control(pitch_speed);
            //roll_speed = pidRoll.Control(roll_speed);


            // Large gyro 3.36E+07
            // Small gyro 448000
            var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs             

            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, controller.WorldMatrix);

            foreach (var gy in Blocks) {
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
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(controller.WorldMatrix));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Down, flattenedTargetVector);
            if (localTargetVector.Z > 0)
                pitch = -pitch;

            roll = MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X > 0)
                roll = -roll;
        }

        /// <summary>
        /// by Whiplash141
        /// Computes angle between 2 vectors
        /// </summary>
        public static double AngleBetween(Vector3D a, Vector3D b) //returns radians
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;
            else
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }
        /*
        /// Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        Dependencies: VectorMath
        * Fix to solve for zero cases when a vertical target vector is input
        * Fixed straight up case
        * Fixed sign on straight up case
        * Converted math to local space
        */
        void getRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double yaw, out double pitch) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = AngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive

            if (Math.Abs(pitch) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                pitch = Math.PI;

            if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                yaw = MathHelper.PiOver2 * Math.Sign(localTargetVector.X);
            else
                yaw = AngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
        }
    }
}
