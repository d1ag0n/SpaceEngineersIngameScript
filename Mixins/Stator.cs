using System;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    class Stator {
        const float epsilon = 0.1f * (MathHelper.Pi / 180.0f);
        const float lockStep = 2.0f * (MathHelper.Pi / 180.0f);
        const float hingeUpper = 90.0f * (MathHelper.Pi / 180.0f);
        const float hingeLower = -90.0f * (MathHelper.Pi / 180.0f);
        

        /// <summary>
        /// offset in radians + or - 
        /// </summary>
        public float mfOffset = 0;//22.5f * (MathHelper.Pi / 180.0f);
        /// <summary>
        /// Does not apply to hinge, when true will point with the position opposite of offset when that position is closer
        /// </summary>
        public bool reverse = false;
        /// <summary>
        /// Does not apply to hinge, if the opposite side is being used to point
        /// </summary>
        public bool isReverse { get; private set; }
        
        public IMyAttachableTopBlock Top => stator.Top;
        public IMyCubeGrid TopGrid => stator.TopGrid;
        public float SpeedFactor = 0.5f;
        public float Angle => stator.Angle;
        public MatrixD WorldMatrix => stator.WorldMatrix;
        public float Displacement => stator.Displacement;
        readonly bool hinge;
        readonly bool softHinge;

        readonly float softLower;
        readonly float softUpper;

        public enum StatorMode {
            point,
            hold
        }
        public readonly IMyMotorStator stator;
        readonly Logger g;
        Vector3D mvTarget;
        float mfTarget;
        public StatorMode Mode { get; private set; }
        bool held;

        /// <summary>
        /// Using the limits will cause the rotor to behave like a hinge
        /// all provided limits limit the 0 position of the rotor in radians, values outside of > 0 < pi are clamped
        /// </summary>
        /// <param name="aStator"></param>
        /// <param name="aLogger"></param>
        /// <param name="aLower">Lower limit > 0 < pi</param>
        /// <param name="aUpper">upper limit > 0 < pi</param>
        public Stator(IMyMotorStator aStator, Logger aLogger, float aLower = 0, float aUpper = 0) {
            stator = aStator;
            g = aLogger;
            softLower = aLower;
            softUpper = aUpper;
            softHinge = true;
        }
        public Stator(IMyMotorStator aStator, Logger aLogger) {
            stator = aStator;
            g = aLogger;
            hinge = aStator.BlockDefinition.SubtypeId == "LargeHinge";
            aStator.ShowOnHUD = true;
            Go();
        }
        public void Info() {
            g.log("CustomName        ", stator.CustomName);
            g.log("Angle             ", stator.Angle);
            g.log("TargetVelocityRad ", stator.TargetVelocityRad);
            //g.log("Upper             ", stator.UpperLimitRad);
            //g.log("Lower             ", stator.LowerLimitRad);
            //g.log("Torque            ", stator.Torque);
            //g.log("BrakingTorque     ", stator.BrakingTorque);
            //g.log("Displacement      ", stator.Displacement);
            g.log("held              ", held);
            g.log("mode              ", Mode);
            g.log("mfTarget          ", mfTarget);
            g.log("mfOffset          ", mfOffset);
            //g.log("mvTarget          ", mvTarget);
        }
        void Go() {
            stator.TargetVelocityRad = 0;
            if (stator.RotorLock) {
                stator.RotorLock = false;
            }
            if (!stator.Enabled) {
                stator.Enabled = true;
            }
            held = false;
        }
        void Stop() {
            stator.TargetVelocityRad = 0;
            stator.UpperLimitRad = stator.Angle;
            stator.LowerLimitRad = stator.Angle;
            held = true;
        }
        /// <summary>
        /// target angle -pi to pi radians away from offset
        /// </summary>
        /// <param name="aTarget"></param>
        public void SetTarget(float aTarget) {
            mfTarget = aTarget;
            Mode = StatorMode.hold;
            Go();
        }
        /// <summary>
        /// set a target in world space
        /// </summary>
        /// <param name="aTarget"></param>
        public void SetTarget(Vector3D aTarget) {
            mvTarget = aTarget;
            Mode = StatorMode.point;
            Go();
        }

        /// <summary>
        /// returns true when the target is reached
        /// when target is an angle the stator will lock when the angle is within epsilon
        /// </summary>
        /// <returns></returns>
        public bool Update() {
            if (!held) {
                if (Mode == StatorMode.hold) {
                    return setStatorAngle(mfTarget);
                } else if (Mode == StatorMode.point) {
                    return pointAtTarget();
                } else {
                    stator.TargetVelocityRad = 0;
                }
            }
            return true;
        }

        void getTargetVectors(out Vector3D local, out Vector3D localFlat) {
            local = MAF.world2pos(mvTarget, stator.WorldMatrix);
            localFlat = new Vector3D(local.X, 0, local.Z);
        }
        bool pointAtTarget() {
            Vector3D local, localFlat;
            getTargetVectors(out local, out localFlat);
            if (hinge) {
                
                float angle = (float)MAF.angleBetween(Vector3D.Left, localFlat);
                if (localFlat.Z > 0) {
                    angle = -angle;
                }
                var error = angle - stator.Angle;
                var result = Math.Abs(error) < epsilon;
                stator.TargetVelocityRad = (error) * SpeedFactor;
                limiter();
                return result;
            } else {
                var ab = MAF.angleBetween(Vector3D.Backward, localFlat);
                g.log("ab ", ab);
                
                if (local.X > 0) {
                    ab = MathHelper.TwoPi - ab;
                }
                g.log("ab ", ab);

                return setStatorAngle((float)ab);
            }
        }
            
        bool setStatorAngle(float aAngle) {
            aAngle += mfOffset;            
            aAngle -= stator.Angle;
            MathHelper.LimitRadians(ref aAngle);
            
            if (aAngle > MathHelper.Pi) {
                aAngle -= MathHelper.TwoPi;
            } else if (aAngle < -MathHelper.Pi) {
                aAngle += MathHelper.TwoPi;
            }

            isReverse = false;
            if (reverse) {
                if (aAngle < -MathHelper.PiOver2) {
                    isReverse = true;
                    aAngle += MathHelper.Pi;
                } else if (aAngle > MathHelper.PiOver2) {
                    isReverse = true;
                    aAngle -= MathHelper.Pi;
                }
            }
            bool result = Math.Abs(aAngle) < epsilon;
            if (result) {
                Stop();
            } else {
                stator.TargetVelocityRad = aAngle * SpeedFactor;
                limiter();
            }
            return result;
        }
        void limiter() {

            //var upper = stator.Angle + lockStep;
            //var lower = stator.Angle - lockStep;
            float upper, lower;
            var velo = stator.TargetVelocityRad;
            var a = stator.Angle;
            if (velo > 0) {
                lower = a - epsilon;
                upper = a + velo;
            } else if (stator.TargetVelocityRad < 0) {
                upper = stator.Angle + epsilon;
                lower = a + velo;
            } else {
                upper = lower = stator.Angle;
            }

            if (hinge) {
                upper = MathHelper.Clamp(upper, hingeLower, hingeUpper);
                lower = MathHelper.Clamp(lower, hingeLower, hingeUpper);
            }

            stator.UpperLimitRad = upper;
            stator.LowerLimitRad = lower;
        }
        /*
        void setHingeAngle(float aAngle) {
            hingeClose = setStator(hinge, aAngle - hinge.Angle);
            if (parent == null || parent.stopped) {
                if (hinge.Angle > hinge.LowerLimitRad && hinge.Angle < aAngle) {
                    hinge.LowerLimitRad = hinge.Angle;
                }
                if (hinge.Angle < hinge.UpperLimitRad && hinge.Angle > aAngle) {
                    hinge.UpperLimitRad = hinge.Angle;
                }
            }
        }
        */
    }
}
