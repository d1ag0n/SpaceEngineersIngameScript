using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public class GyroModule : Module<IMyGyro> {
        const double min = 0.001;
        double difMax = 0.09;   // angular velocity difference threshold
        double slowFact = 20.0; // slow down factor how quickly the ship tries to slow down toward the end of a turn
        double fastFact = 2.0;  // speed up factor
        double smallMax = 0.4;  // angle remaining in turn when smallFactor is applied
        double smallFact = 1.9; // factor applied when within smallMax

        public new bool Active {
            get { return base.Active; }
            private set {
                if (base.Active != value) {
                    base.Active = value;
                    init();
                }
            }
        }

        Vector3D mTargetPosition;
        Vector3D mTargetDirection;
        bool calcDirection = false;

        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        public GyroModule(ModuleManager aManager) : base(aManager) {
            MenuName = "Gyroscope";
            onUpdate = UpdateAction;
            onSave = SaveAction;
            onLoad = LoadAction;
            Active = true;
            
            init();
            onPage = p => {
                if (onUpdate != UpdateAction) {
                    onUpdate = UpdateAction;
                    init();
                }
                mMenuItems.Clear();
                mMenuItems.Add(new MenuItem(Active ? "Activate" : "Deactivate", Nactivate));
                mMenuItems.Add(new MenuItem("Configurator", null, ConfigAction));
                return mMenuItems;
            };
        }

        void SaveAction(Serialize s) {

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

        void LoadAction(Serialize s, string aData) {
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
            if (mTargetPosition != aWorld) {
                Active = true;
                mTargetPosition = aWorld;
            }
        }

        public float Roll;
        public float Yaw;
        Vector3D _RollTarget;
        public void SetRollTarget(Vector3D aWorld) {
            _RollTarget = aWorld;
        }

        public void SetTargetDirection(Vector3D aWorld) {
            calcDirection = false;
            if (mTargetDirection != aWorld) {
                Active = true;
                mTargetDirection = aWorld;
            }
        }
        public IMyTerminalBlock NavBlock;


        string strRaiseDifMax => $"Increase max difference for AV correction: {difMax.ToString("f4")}";
        void  raiseDifMax() => difMax *= 1.1;
        string strLowerDifMax => $"Decrease max difference for AV correction: {difMax.ToString("f4")}";
        void lowerDifMax() {
            difMax *= 0.9;
            if (difMax < min) {
                difMax = min;
            }
        }

        string strRaiseFastFact => $"Increase AV acceleration factor:           {fastFact.ToString("f4")}";
        void raiseFastFact() => fastFact*= 1.1;
        string strLowerFastFact => $"Decrease AV acceleration factor:           {fastFact.ToString("f4")}";
        void lowerFastFact() {
            fastFact *= 0.9;
            if (fastFact < min) {
                fastFact = min;
            }
        }

        string strRaiseSlowFact => $"Increase AV deceleration factor:           {slowFact.ToString("f4")}";
        void raiseSlowFact() => slowFact*= 1.1;
        string strLowerSlowFact => $"Decrease AV deceleration factor:           {slowFact.ToString("f4")}";
        void  lowerSlowFact() {
            slowFact *= 0.9;
            if (slowFact < min) {
                slowFact = min;
            }
        }

        string strRaiseSmallMax => $"Increase small turn size:                {smallMax.ToString("f4")}";
        void raiseSmallMax() => smallMax *= 1.1;
        string strLowerSmallMax => $"Decrease small turn size:                {smallMax.ToString("f4")}";
        void lowerSmallMax() {
            smallMax *= 0.9;
            if (smallMax < min) {
                smallMax = min;
            }
        }
        string strRaiseSmallFact => $"Increase factor applied to small turns:  {smallFact.ToString("f4")}";
        void raiseSmallFact() => smallFact *= 1.1;
        string strLowerSmallFact => $"Decrease factor applied to small turns:  {smallFact.ToString("f4")}";
        void lowerSmallFact() {
            smallFact *= 0.9;
            if (smallFact < min) {
                smallFact = min;
            }
        }

        List<MenuItem> configMenu(int page) {
            mMenuItems.Clear();
            if (page == 0) {
                mMenuItems.Add(new MenuItem(strRaiseDifMax, raiseDifMax));
                mMenuItems.Add(new MenuItem(strLowerDifMax, lowerDifMax));
                mMenuItems.Add(new MenuItem(strRaiseFastFact, raiseFastFact));
                mMenuItems.Add(new MenuItem(strLowerFastFact, lowerFastFact));
                mMenuItems.Add(new MenuItem(strRaiseSlowFact, raiseSlowFact));
                mMenuItems.Add(new MenuItem(strLowerSlowFact, lowerSlowFact));
            } else if (page == 1) {
                mMenuItems.Add(new MenuItem(strRaiseSmallMax, raiseSmallMax));
                mMenuItems.Add(new MenuItem(strLowerSmallMax, lowerSmallMax));
                mMenuItems.Add(new MenuItem(strRaiseSmallFact, raiseSmallFact));
                mMenuItems.Add(new MenuItem(strLowerSmallFact, lowerSmallFact));
                mMenuItems.Add(new MenuItem("Set Default Values", () => {
                    difMax = 0.09;
                    slowFact = 20.0;
                    fastFact = 2.0;
                    smallMax = 0.4;
                    smallFact = 1.9;
                }));
            }
            return mMenuItems;

        }
        Vector3D configDir;
        int configCount = 0;

        Menu ConfigAction(MenuModule aMain, object argument) {
            configCount = 0;
            configDir = Vector3D.Up;
            Active = true;
            init();
            onUpdate = () => {
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

        void Nactivate() {
            Active = !Active;
            init();
            var emm = mMenuItems[0];
            mMenuItems[0] = new MenuItem(Active ? "Deactivate" : "Activate", emm.State, emm.Method);
        }
        public override bool Accept(IMyTerminalBlock b) {
            var result = base.Accept(b);
            if (result) {
                init(b as IMyGyro);
            }
            return result;
        }


        
        void UpdateAction() {
            if (!Active) {
                return;
            }
            var sc = controller.Remote;
            if (sc == null) {
                init();
                return;
            }
            var m = NavBlock == null ? controller.Remote.WorldMatrix : NavBlock.WorldMatrix;
            if (calcDirection) {
                if (mTargetPosition.IsZero()) {
                    mTargetDirection = Vector3D.Zero;
                } else {
                    mTargetDirection = Vector3D.Normalize(mTargetPosition - m.Translation);
                }
            }
            if (mTargetDirection.IsZero()) {
                init();
                return;
            }

            double pitch, yaw;

            var sv = controller.ShipVelocities;
            var av = MAF.world2dir(sv.AngularVelocity, m);

            
            MAF.getRotationAngles(mTargetDirection, m, out yaw, out pitch);


            if (Math.Abs(pitch) < smallMax) {
                pitch *= smallFact;
            }

            if (Math.Abs(yaw) < smallMax) {
                yaw *= smallFact;
            }

            var pv = av.X;
            var yv = -av.Y;

            var pitchDif = (pitch - pv);
            var yawDif = (yaw - yv);

            if (Math.Abs(pitchDif) < difMax) {
                pitchDif = 0;
            } else {
                if (pitch < 0) {
                    if (pv > pitch) {
                        pitchDif *= fastFact;
                    } else {
                        pitchDif *= slowFact;
                    }
                } else {
                    if (pv < pitch) {
                        pitchDif *= fastFact;
                    } else {
                        pitchDif *= slowFact;
                    }
                }
            }
            if (Math.Abs(yawDif) < difMax) {
                yawDif = 0;
            } else {
                if (yaw < 0) {
                    if (yv > yaw) {
                        yawDif *= fastFact;
                    } else {
                        yawDif *= slowFact;
                    }
                } else {
                    if (yv < yaw) {
                        yawDif *= fastFact;
                    } else {
                        yawDif *= slowFact;
                    }
                }
            }
            pitch += pitchDif;
            yaw += yawDif;
            if (!_RollTarget.IsZero()) {
                //var ab = (float)MAF.angleBetween(m.Down, Vector3D.Normalize(_RollTarget - m.Translation));
                double rp, rr;
                var dir = Vector3D.Normalize(_RollTarget - MyMatrix.Translation);
                MAF.getRotationAnglesFromDown(m, dir, out rp, out rr);
                logger.log($"Roll={rr}");
                Roll = (float)rr;
            }
            applyGyroOverride(m, pitch, Yaw == 0f ? yaw : Yaw, Roll);
        }

        public void setGyrosEnabled(bool aValue) {
            foreach (var gy in Blocks) {
                gy.Enabled = aValue;
            }
        }
        public void init() {
            foreach (var gy in Blocks) init(gy);
        }
        void init(IMyGyro aGyro) {

            if (!aGyro.Enabled) {
                aGyro.Enabled = true;
            }
            if (!aGyro.GyroOverride) {
                aGyro.GyroOverride = true;
            }
            aGyro.ShowInTerminal = 
            aGyro.ShowOnHUD = false;
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
  

        
        
    }
}
