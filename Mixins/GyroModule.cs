using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class GyroModule : Module<IMyGyro> {


        /*
        const double updatesPerSecond = 6.0;
        double proportionalConstant = 2.5;
        double integralConstant = 0.1;
        double derivativeConstant = 1.0;
        double pidLimit = 10;
        const double timeLimit = 1.0 / updatesPerSecond;
        PID pidPitch;
        PID pidRoll;
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
        */
        

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
            //initPid();
            
        }
        
        public override bool Accept(IMyTerminalBlock b) {
            var result = base.Accept(b);
            if (result) {
                init(b as IMyGyro);
            }
            return result;
        }
        public void Rotate(Vector3D aDesiredDown) {

            //var av = sv.AngularVelocity;


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

            // local angular velocity
            // +X = +pitch
            // -X = -pitch
            // +Y = -yaw
            // -Y = +yaw
            // +Z = -roll
            // -Z = +roll

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

            double pitch, roll;

            var sv = controller.GetShipVelocities();
            var av = MAF.world2dir(sv.AngularVelocity, controller.WorldMatrix);

            getRotationAnglesFromDown(aDesiredDown, out pitch, out roll);

            var min = 0.1;
            var pitchDif = pitch - av.X;

            if (Math.Abs(pitchDif) < min) {
                pitchDif = 0;
            }

            var rollDif = roll + av.Z;
            
            if (Math.Abs(rollDif) < min) {
                rollDif = 0;
            }

            var fact = 6.0;
            applyGyroOverride(pitch + pitchDif * fact, av.Y, roll + rollDif * fact);
            
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
    }
}
