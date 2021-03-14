using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{
    class GyroModule : Module<IMyGyro> {
        const double min = 0.001;
        double difMax = 0.09;   // angular velocity difference threshold
        double slowFact = 20.0; // slow down factor how quickly the ship tries to slow down toward the end of a turn
        double fastFact = 2.0;  // speed up factor
        double smallMax = 0.4;  // angle remaining in turn when smallFactor is applied
        double smallFact = 1.9; // factor applied when within smallMax

        Vector3D mTargetPosition;
        Vector3D mTargetDirection;
        bool calcDirection = false;

        readonly List<object> mainMenuMethods = new List<object>();
        public GyroModule() {
            MenuName = "Gyroscope";
            Update = UpdateAction;
            Save = SaveDel;
            Load = LoadDel;
            mainMenu();
            Nactivate(null, null);
            Menu = p => {
                if (Update != UpdateAction) {
                    Update = UpdateAction;
                    init();
                }
                return mainMenuMethods;
            };
        }

        void SaveDel(Serialize s) {

            s.unt("difMax");
            s.str(difMax);
            s.rec();

            s.unt("slowFact");
            s.str(slowFact);
            s.rec();

            s.unt("fastFact");
            s.str(fastFact);
            s.rec();

            s.unt("smallMax");
            s.str(smallMax);
            s.rec();

            s.unt("smallFact");
            s.str(smallFact);

        }

        void LoadDel(Serialize s, string aData) {
            var ar = aData.Split(Serialize.RECSEP);
            foreach (var record in ar) {
                var entry = record.Split(Serialize.UNTSEP);
                switch (entry[0]) {
                    case "difMax":
                        double.TryParse(entry[1], out difMax);
                        break;
                    case "slowFact":
                        double.TryParse(entry[1], out slowFact);
                        break;
                    case "fastFact":
                        double.TryParse(entry[1], out fastFact);
                        break;
                    case "smallMax":
                        double.TryParse(entry[1], out smallMax);
                        break;
                    case "smallFact":
                        double.TryParse(entry[1], out smallFact);
                        break;
                }
            }
        }

        public void SetTargetPosition(Vector3D aWorld) {
            calcDirection = true;
            mTargetPosition = aWorld;
        }
        
        public void SetTargetDirection(Vector3D aWorld) {
            calcDirection = false;
            mTargetDirection = aWorld;
        }
        void mainMenu() {
            mainMenuMethods.Clear();
            mainMenuMethods.Add(new MenuMethod("Activate", null, Nactivate));
            mainMenuMethods.Add(new MenuMethod("Configurator", null, ConfigAction));
        }

        string strRaiseDifMax => $"Increase max difference for AV correction: {difMax.ToString("f4")}";
        Menu raiseDifMax(MenuModule aMain, object argument) {
            difMax *= 1.1;
            return null;
        }
        string strLowerDifMax => $"Decrease max difference for AV correction: {difMax.ToString("f4")}";
        Menu lowerDifMax(MenuModule aMain, object argument) {
            difMax *= 0.9;
            if (difMax < min) {
                difMax = min;
            }
            return null;
        }

        string strRaiseFastFact => $"Increase AV acceleration factor:           {fastFact.ToString("f4")}";
        Menu raiseFastFact(MenuModule aMain, object argument) {
            fastFact*= 1.1;
            return null;
        }
        string strLowerFastFact => $"Decrease AV acceleration factor:           {fastFact.ToString("f4")}";
        Menu lowerFastFact(MenuModule aMain, object argument) {
            fastFact *= 0.9;
            if (fastFact < min) {
                fastFact = min;
            }
            return null;
        }

        string strRaiseSlowFact => $"Increase AV deceleration factor:           {slowFact.ToString("f4")}";
        Menu raiseSlowFact(MenuModule aMain, object argument) {
            slowFact*= 1.1;
            return null;
        }
        string strLowerSlowFact => $"Decrease AV deceleration factor:           {slowFact.ToString("f4")}";
        Menu lowerSlowFact(MenuModule aMain, object argument) {
            slowFact *= 0.9;
            if (slowFact < min) {
                slowFact = min;
            }
            return null;
        }

        string strRaiseSmallMax => $"Increase small turn size:                {smallMax.ToString("f4")}";
        Menu raiseSmallMax(MenuModule aMain, object argument) {
            smallMax *= 1.1;
            return null;
        }
        string strLowerSmallMax => $"Decrease small turn size:                {smallMax.ToString("f4")}";
        Menu lowerSmallMax(MenuModule aMain, object argument) {
            smallMax *= 0.9;
            if (smallMax < min) {
                smallMax = min;
            }
            return null;
        }
        string strRaiseSmallFact => $"Increase factor applied to small turns:  {smallFact.ToString("f4")}";
        Menu raiseSmallFact(MenuModule aMain, object argument) {
            smallFact *= 1.1;
            return null;
        }
        string strLowerSmallFact => $"Decrease factor applied to small turns:  {smallFact.ToString("f4")}";
        Menu lowerSmallFact(MenuModule aMain, object argument) {
            smallFact *= 0.9;
            if (smallFact < min) {
                smallFact = min;
            }
            return null;
        }

        List<object> configMenu(int page) {
            var result = new List<object>();
            if (page == 0) {
                result.Add(new MenuMethod(strRaiseDifMax, null, raiseDifMax));
                result.Add(new MenuMethod(strLowerDifMax, null, lowerDifMax));
                result.Add(new MenuMethod(strRaiseFastFact, null, raiseFastFact));
                result.Add(new MenuMethod(strLowerFastFact, null, lowerFastFact));
                result.Add(new MenuMethod(strRaiseSlowFact, null, raiseSlowFact));
                result.Add(new MenuMethod(strLowerSlowFact, null, lowerSlowFact));
            } else if (page == 1) {
                result.Add(new MenuMethod(strRaiseSmallMax, null, raiseSmallMax));
                result.Add(new MenuMethod(strLowerSmallMax, null, lowerSmallMax));
                result.Add(new MenuMethod(strRaiseSmallFact, null, raiseSmallFact));
                result.Add(new MenuMethod(strLowerSmallFact, null, lowerSmallFact));
                result.Add(new MenuMethod("Set Default Values", null, (m, a) => {
                    difMax = 0.09;
                    slowFact = 20.0;
                    fastFact = 2.0;
                    smallMax = 0.4;
                    smallFact = 1.9;
                    return null;
                }));
            }
            return result;

        }
        Vector3D configDir;
        int configCount = 0;

        Menu ConfigAction(MenuModule aMain, object argument) {
            configCount = 0;
            configDir = Vector3D.Up;
            Active = true;
            init();
            Update = () => {
                logger.log("config update");
                if (controller.ShipVelocities.AngularVelocity.LengthSquared() < 1 && MAF.angleBetween(controller.Remote.WorldMatrix.Forward, configDir) < 0.01) {
                    logger.log("config waiting");
                    init();
                    configCount++;
                    if (configCount == 18) {
                        configDir = -configDir;
                    }
                } else {
                    logger.log("config turning");
                    configCount = 0;
                    SetTargetDirection(configDir);
                    UpdateAction();
                }
            };
            return new Menu(aMain, "Angular Velocity Configurator", configMenu);
        }




        
        Menu Nactivate(MenuModule aMain, object argument) {
            Active = !Active;
            init();
            var emm = (MenuMethod)mainMenuMethods[0];
            mainMenuMethods[0] = new MenuMethod(Active ? "Deactivate" : "Activate", emm.State, emm.Method);
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
            var sc = controller.Remote;
            if (sc == null) {
                init();
                return;
            }
            if (calcDirection) {
                if (mTargetPosition.IsZero()) {
                    mTargetDirection = Vector3D.Zero;
                } else {
                    mTargetDirection = Vector3D.Normalize(mTargetPosition - sc.WorldMatrix.Translation);
                }
            }
            if (mTargetDirection.IsZero()) {
                init();
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

            double pitch, yaw;

            var sv = controller.ShipVelocities;
            var av = MAF.world2dir(sv.AngularVelocity, sc.WorldMatrix);

            
            //getRotationAnglesFromDown(direction, sc.WorldMatrix, out pitch, out roll);
            getRotationAngles(mTargetDirection, sc.WorldMatrix, out yaw, out pitch);

            logger.log($"pitch = {pitch}");
            logger.log($"velo  = {av.X}");

            logger.log($"yaw  = {yaw}");
            logger.log($"velo  = {-av.Y}");

            if (Math.Abs(pitch) < smallMax) {
                logger.log("pitch smallfact");
                pitch *= smallFact;
            } else {
                logger.log("pitch normal fact");
            }

            if (Math.Abs(yaw) < smallMax) {
                logger.log("yaw smallfact");
                yaw *= smallFact;
            } else {
                logger.log("yaw normal fact");
            }

            //yaw *= 10.0;
            //pitch *= 10.0;
            //getRotationAnglesFromForward(direction, sc.WorldMatrix, out pitch, out roll);
            //applyGyroOverride(sc.WorldMatrix, pitch, yaw, roll);


            var pv = av.X;
            var yv = -av.Y;
            //return;
            



            //double pitchDif = (pitch - av.X);
            //double yawDif = (yaw + av.Y);

            var pitchDif = (pitch - pv);
            var yawDif = (yaw - yv);

            if (Math.Abs(pitchDif) < difMax) {
                pitchDif = 0;
                logger.log("pitch okay");
            } else {
                if (pitch < 0) {
                    if (pv > pitch) {
                        logger.log("pitch too slow");
                        pitchDif *= fastFact;
                    } else {
                        logger.log("pitch too fast");
                        pitchDif *= slowFact;
                    }
                } else {
                    if (pv < pitch) {
                        logger.log("pitch too slow");
                        pitchDif *= fastFact;
                    } else {
                        logger.log("pitch too fast");
                        pitchDif *= slowFact;
                    }
                }
            }
            logger.log($"pitchDIf = {pitchDif}");

            if (Math.Abs(yawDif) < difMax) {
                yawDif = 0;
                logger.log("yaw okay");
            } else {
                if (yaw < 0) {
                    if (yv > yaw) {
                        logger.log("yaw too slow");
                        yawDif *= fastFact;
                    } else {
                        logger.log("yaw too fast");
                        yawDif *= slowFact;
                    }
                } else {
                    if (yv < yaw) {
                        logger.log("yaw too slow");
                        yawDif *= fastFact;
                    } else {
                        logger.log("yaw too fast");
                        yawDif *= slowFact;
                    }
                }
            }
            logger.log($"yawDIf = {yawDif}");

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
            yaw += yawDif;            
            
            applyGyroOverride(sc.WorldMatrix, pitch, yaw, 0);
            
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
        void init() {
            foreach (var gy in Blocks) init(gy);
        }
        void init(IMyGyro aGyro) {

            if (!aGyro.Enabled) {
                aGyro.Enabled = true;
            }
            if (!aGyro.GyroOverride) {
                aGyro.GyroOverride = true;
            }
            aGyro.Yaw = 0;
            aGyro.Pitch = 0;
            aGyro.Roll = 0;
            if (!Active) {
                aGyro.GyroOverride = false;
            }
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
        getRotationAnglesFromDown - modified by d1ag0n for pitch and roll
        Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        */
        static void getRotationAnglesFromDown(Vector3D targetVector, MatrixD worldMatrix, out double pitch, out double roll) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            //var localTargetVector = MAF.world2pos(targetVector, worldMatrix);
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


        /*
        /// Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        Dependencies: VectorMath
        * Fix to solve for zero cases when a vertical target vector is input
        * Fixed straight up case
        * Fixed sign on straight up case
        * Converted math to local space
        *//**
        static void getRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double yaw, out double pitch) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive

            if (Math.Abs(pitch) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                pitch = Math.PI;

            if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                yaw = MathHelper.PiOver2 * Math.Sign(localTargetVector.X);
            else
                yaw = MAF.angleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
        }//*/
        /*
        getRotationAnglesFromForward - modified by d1ag0n
        Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        */
        void getRotationAngles(Vector3D direction, MatrixD worldMatrix, out double yaw, out double pitch) {
            logger.log("From Forward");
            var localTargetVector = MAF.world2dir(direction, worldMatrix);
            var flattenedTargetVector = new Vector3D(0, localTargetVector.Y, localTargetVector.Z);

            pitch = MAF.angleBetween(Vector3D.Forward, flattenedTargetVector);
            if (localTargetVector.Y < 0)
                pitch = -pitch;

            yaw = MAF.angleBetween(localTargetVector, flattenedTargetVector);
            if (localTargetVector.X < 0)
                yaw = -yaw;
        }
    }
}
