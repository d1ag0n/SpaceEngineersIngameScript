﻿using System;
using System.Collections.Generic;
using System.Text;
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
        /// Does not apply to hinge, when true will point with the side opposite of offset when that side is closer
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

        public enum StatorMode {
            point,
            hold,
            idle
        }
        public readonly IMyMotorStator stator;
        readonly Logger g;
        Vector3D mvTarget;
        float mfTarget;
        public StatorMode Mode { get; private set; }
        bool held;

        public Stator(IMyMotorStator aStator, Logger aLogger) {
            stator = aStator;
            g = aLogger;
            if (hinge = aStator.BlockDefinition.SubtypeId == "LargeHinge") {

            } else {
                //g.persist(aStator.BlockDefinition.SubtypeId);
            }
            aStator.ShowOnHUD = true;
            Go();
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
            g.log("mode              ", Mode);
            g.log("mfTarget          ", mfTarget);
            g.log("mvTarget          ", mvTarget);
        }
        public void Go() {

            stator.TargetVelocityRad = 0;
            if (hinge) {
                if (stator.UpperLimitRad != hingeUpper) {
                    stator.UpperLimitRad = hingeUpper;
                }
                if (stator.LowerLimitRad != hingeLower) {
                    stator.LowerLimitRad = hingeLower;
                }
            } else {
                if (stator.UpperLimitRad != float.MaxValue) {
                    stator.UpperLimitRad = float.MaxValue;
                }
                if (stator.LowerLimitRad != float.MinValue) {
                    stator.LowerLimitRad = float.MinValue;
                }
            }
            if (stator.RotorLock) {
                stator.RotorLock = false;
            }
            if (!stator.Enabled) {
                stator.Enabled = true;
            }
            held = false;
        }
        public void Stop() {
            g.persist("stator stopped");
            stator.TargetVelocityRad = 0;
            stator.UpperLimitRad = stator.Angle;
            stator.LowerLimitRad = stator.Angle;
            held = true;
        }
        public void SetTarget(float aTarget) {
            mfTarget = aTarget;
            Mode = StatorMode.hold;
        }
        public void SetTarget(Vector3D aTarget) {
            mvTarget = aTarget;
            Mode = StatorMode.point;
        }
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
            var targetVector = Vector3D.Normalize(mvTarget - stator.WorldMatrix.Translation);
            local = MAF.world2dir(targetVector, stator.WorldMatrix);
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
                if (local.X > 0) {
                    ab = MathHelperD.TwoPi - ab;
                }
                return setStatorAngle((float)ab);
            }
        }
            
        bool setStatorAngle(float aAngle) {
            aAngle += mfOffset;            
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
                    aAngle += MathHelper.Pi;
                } else if (aAngle > MathHelper.PiOver2) {
                    isReverse = true;
                    aAngle -= MathHelper.Pi;
                }
            }
            var angleAbs = Math.Abs(aAngle);
            bool result = Math.Abs(aAngle) < epsilon;
            if (result) {
                Go();
                //stator.UpperLimitRad = stator.Angle;
                //stator.LowerLimitRad = stator.Angle;
            } else {
                stator.TargetVelocityRad = aAngle * SpeedFactor;
                limiter();
            }
            return result;
        }
        void limiter() {
            return;
            var upper = stator.Angle + lockStep;
            var lower = stator.Angle - lockStep;

            if (stator.TargetVelocityRad > 0) {
                lower = stator.Angle - epsilon;
            } else if (stator.TargetVelocityRad < 0) {
                upper = stator.Angle + epsilon;
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