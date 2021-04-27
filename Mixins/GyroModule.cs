using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public partial class GyroModule : Module<IMyGyro> {

        readonly ShipControllerModule mController;
        
        double difMax = 0.06;   // angular velocity difference threshold
        double slowFact = 20.0; // slow down factor how quickly the ship tries to slow down toward the end of a turn
        double fastFact = 2.0;  // speed up factor
        double smallMax = 0.4;  // angle remaining in turn when smallFactor is applied
        double smallFact = 1.9; // factor applied when within smallMax

        Vector3D mTargetPosition;
        Vector3D mTargetDirection;
        bool calcDirection = false;
        public float MaxNGVelo = 0;

        public new bool Active {
            get { return base.Active; }
            set {
                if (base.Active != value) {
                    base.Active = value;
                    init();
                }
            }
        }
        
        public GyroModule(ModuleManager aManager) : base(aManager) {
            aManager.GetModule(out mController);
            onUpdate = UpdateAction;
            Active = true;
            init();
        }

        /*void SaveAction(Serialize s) {

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

        }*/

        /*void LoadAction(Serialize s, string aData) {
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
        }*/

        public void SetTargetPosition(Vector3D aWorld) {
            calcDirection = true;
            if (mTargetPosition != aWorld) {
                Active = true;
                mTargetPosition = aWorld;
            }
        }

        public float Roll;
        //public float Yaw;
        Vector3D _RollTarget;
        public void SetRollTarget(Vector3D aWorld) {
            _RollTarget = aWorld;
        }

        public void SetTargetDirection(Vector3D aWorld) {
            calcDirection = false;
            Active = true;
            if (MAF.nearEqual(mTargetDirection, aWorld)) {
                init();
            } else {
                
                mTargetDirection = aWorld;
            }
        }
        public IMyTerminalBlock NavBlock;

        public override bool Accept(IMyTerminalBlock b) {
            var result = base.Accept(b);
            if (result) {
                init(b as IMyGyro);
            }
            return result;
        }

        void UpdateAction() {
            var sc = mController.Remote;
            if (sc == null) {
                init();
                return;
            }
            var m = NavBlock == null ? mController.Remote.WorldMatrix : NavBlock.WorldMatrix;
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

            var sv = mController.ShipVelocities;
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
                Roll = (float)rr;
            }
            
            applyGyroOverride(m, pitch, yaw, Roll);
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
            mLog.log($"MaxNGVelo={MaxNGVelo}");
            if (MaxNGVelo > 0) {
                
                pitch = MathHelperD.Clamp(pitch, -MaxNGVelo, MaxNGVelo);
                yaw = MathHelperD.Clamp(yaw, -MaxNGVelo, MaxNGVelo);
                roll = MathHelperD.Clamp(roll, -MaxNGVelo, MaxNGVelo);
            }

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
