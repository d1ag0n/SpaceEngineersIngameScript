using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{
    class GyroModule : Module<IMyGyro> {
        public Vector3D Target;
        readonly List<object> menuMethods = new List<object>();
        public GyroModule() {
            MenuName = "Gyroscope";
            Update = UpdateAction;
            menuMethods.Add(new MenuMethod("Activate", null, Nactivate));
        }
        public Menu Nactivate(MenuModule aMain = null, object argument = null) {
            Active = !Active;
            ((MenuMethod)menuMethods[0]).Name = Active ? "Deactivate" : "Activate";
            return null;
        }
        public override bool Accept(IMyTerminalBlock b) {
            var result = base.Accept(b);
            if (result) {
                init(b as IMyGyro);
            }
            return result;
        }
        
        void UpdateAction() {
            if (!Active) return;
            if (Target.IsZero()) {
                foreach(var gy in Blocks) {
                    init(gy);
                }
                return;
            }

            var sc = controller.Remote;
            if (sc == null) {
                sc = controller.Cockpit;
                if (sc == null) {
                    foreach (var gy in Blocks) {
                        init(gy);
                    }
                    return;
                }
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
            getRotationAngles(Target, sc.WorldMatrix, out yaw, out pitch);
            var smallMax = 0.4;
            var smallFact = 1.9;
            if (Math.Abs(pitch) < smallMax) {
                pitch *= smallFact;
            }

            if (Math.Abs(roll) < smallMax) {
                roll *= smallFact;
            }

            var sv = controller.ShipVelocities;
            var av = MAF.world2dir(sv.AngularVelocity, sc.WorldMatrix);

            var pitchDif = (pitch - av.X);
            var rollDif = (roll + av.Z);

            var slowFact = 20.0;
            var fastFact = 2.0;
            var difMax = 0.09;
            if (Math.Abs(pitchDif) < difMax) {
                pitchDif = 0;
            } else {
                if (Math.Abs(pitch) < Math.Abs(av.X)) {
                    logger.log("pitch too fast");
                    pitchDif *= slowFact;
                } else {
                    logger.log("pitch too slow");
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

            logger.log("desired pitch = ", pitch);
            logger.log("desired roll  = ", roll);
            logger.log();
            logger.log("velo pitch    = ", av.X);
            logger.log("velo roll     = ", -av.Z);
            logger.log();
            logger.log("diff pitch    = ", pitchDif);
            logger.log("diff roll     = ", rollDif);
            logger.log();
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
            
            applyGyroOverride(sc.WorldMatrix, pitch , yaw, 0);
            
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
        void applyGyroOverride(MatrixD worldMatrix, double pitch, double yaw, double roll) {

            //pitch_speed = pidPitch.Control(pitch_speed);
            //roll_speed = pidRoll.Control(roll_speed);


            // Large gyro 3.36E+07
            // Small gyro 448000
            var rotationVec = new Vector3D(-pitch, yaw, roll); //because keen does some weird stuff with signs             

            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

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
        void getRotationAnglesFromDown(Vector3D targetVector, MatrixD worldMatrix, out double pitch, out double roll) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
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
        /// todo move to maf
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
