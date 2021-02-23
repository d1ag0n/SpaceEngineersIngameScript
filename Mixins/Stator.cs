using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    class Stator {
        const float epsilon = 0.5f * (MathHelper.Pi / 180.0f);
        const float hingeUpper = 110.0f * (MathHelper.Pi / 180.0f);
        const float hingeLower = -110.0f * (MathHelper.Pi / 180.0f);
        /// <summary>
        /// offset in radians + or - 
        /// </summary>
        public float mfOffset = 0;//22.5f * (MathHelper.Pi / 180.0f);
        /// <summary>
        /// Does not apply to hinge, when true will point with the side opposite of offset when that side is closer
        /// </summary>
        public bool reverse = false;
        /// <summary>
        /// Does not apply to hinge, if the opposite side is being used to point
        /// </summary>
        public bool isReverse { get; private set; }

        public float SpeedFactor = 10.0f;
        readonly bool hinge;

        enum StatorMode {
            point,
            hold
        }
        readonly IMyMotorStator stator;
        readonly Logger g;
        Vector3D mvTarget;
        float mfTarget;
        StatorMode meMode = StatorMode.hold;
        bool held;

        public Stator(IMyMotorStator aStator, Logger aLogger) {
            stator = aStator;
            g = aLogger;
            g.persist(aStator.BlockDefinition.SubtypeId);
            if (hinge = aStator.BlockDefinition.SubtypeId == "LargeHinge") {

            }
            reset();
            mfTarget = stator.Angle;
            hold();
        }
        public void Info() {
            g.log("CustomName        ", stator.CustomName);
            g.log("Angle             ", stator.Angle);
            g.log("TargetVelocityRad ", stator.TargetVelocityRad);
            g.log("Upper             ", stator.UpperLimitRad);
            g.log("Lower             ", stator.LowerLimitRad);
            g.log("Torque            ", stator.Torque);
            g.log("BrakingTorque     ", stator.BrakingTorque);
            g.log("Displacement      ", stator.Displacement);
            g.log("held              ", held);
            g.log("mode              ", meMode);
            g.log("mfTarget          ", mfTarget);
            g.log("mvTarget          ", mvTarget);
        }
        void reset() {
            if (hinge) {
                stator.UpperLimitRad = hingeUpper;
                stator.LowerLimitRad = hingeLower;
            } else { 
                stator.UpperLimitRad = float.MaxValue;
                stator.LowerLimitRad = float.MinValue;
            }
            held = false;
        }
        void hold() {
            stator.UpperLimitRad = stator.Angle;
            stator.LowerLimitRad = stator.Angle;
            held = true;
        }
        public void setTarget(float aTarget) {
            if (mfTarget != aTarget) {
                mfTarget = aTarget;
                reset();
                meMode = StatorMode.hold;
            }
        }
        public void setTarget(Vector3D aTarget) {
            if (aTarget != mvTarget) {
                mvTarget = aTarget;
                reset();
                meMode = StatorMode.point;
            }
        }
        public void Update() {
            if (!held) {
                if (meMode == StatorMode.hold) {
                    setStatorAngle(mfTarget);
                } else {
                    pointAtTarget();
                }
            }
        }
        void pointAtTarget() {
            var targetVector = Vector3D.Normalize(mvTarget - stator.WorldMatrix.Translation);
            var local = MAF.world2dir(targetVector, stator.WorldMatrix);
            var localFlat = new Vector3D(local.X, 0, local.Z);


            if (hinge) {
                float angle = (float)MAF.angleBetween(Vector3D.Left, localFlat);
                if (localFlat.Z > 0) {
                    angle = -angle;
                }
                stator.TargetVelocityRad = (angle - stator.Angle) * SpeedFactor;

            } else {
                var ab = MAF.angleBetween(Vector3D.Backward, localFlat);
                if (local.X > 0) {
                    ab = MathHelperD.TwoPi - ab;
                }
                ab += mfOffset;
                if (ab > MathHelperD.TwoPi) {
                    ab -= MathHelperD.TwoPi;
                }
                setStatorAngle((float)ab);
            }
        }
            
        void setStatorAngle(float aAngle) {
            aAngle -= stator.Angle;
            if (aAngle > MathHelper.Pi) {
                aAngle -= MathHelper.TwoPi;
            } else if (aAngle < -MathHelper.Pi) {
                aAngle += MathHelper.TwoPi;
            }
            isReverse = false;
            if (reverse) {
                if (aAngle < -MathHelper.PiOver2) {
                    isReverse = true;
                    aAngle += MathHelper.Pi; // reverse point
                } else if (aAngle > MathHelper.PiOver2) {
                    isReverse = true;
                    aAngle -= MathHelper.Pi; // reverse point
                }
            }
            stator.TargetVelocityRad = aAngle * SpeedFactor;
        }
    }
}
