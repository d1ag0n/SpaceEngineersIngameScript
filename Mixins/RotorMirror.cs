using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {
    public class RotorMirror {
        public readonly IMyMotorStator mStator, mReflection;
        readonly float mOffset;
        public RotorMirror(IMyMotorStator aStator) {
            if (aStator == null) {
                throw new Exception();
            }
            mStator = aStator;
            var pos = mStator.Position;
            var down = Base6Directions.GetOppositeDirection(mStator.Orientation.Up);
            var dir = Base6Directions.GetIntVector(down);
            while (true) {
                pos += dir;
                var b = mStator.CubeGrid.GetCubeBlock(pos);
                if (b == null) {
                    continue;
                }
                var r = b.FatBlock as IMyMotorStator;
                if (r != null) {
                    if (r.Orientation.Up == down) {
                        mReflection = r;
                        mReflection.CustomName = aStator.CustomName + " Reflection";
                        break;
                    }
                }
            }
            var o = mStator.Angle - mReflection.Angle;
            if (Math.Abs(o) > 3) {
                mOffset = MathHelper.Pi;
            } else if (o > 1) {
                mOffset = -MathHelper.PiOver2;
            } else if (o < -1) {
                mOffset = MathHelper.PiOver2;
            }
        }
        Lag lag = new Lag(4);
        void setTorque(float angle) {
            
            var anglePct = Math.Abs(angle) / Program.MAX_ANGLE;
            var totalTorque = (Program.mMaxTorque * Program.MAX_TORQUE) - (Program.mMaxTorque * Program.MIN_TORQUE);
            double t = totalTorque * anglePct;
            lag.Update(t);
            t = lag.Value;
            var f = (float)t;
            mStator.Torque = f;
            mReflection.Torque = f;
        }
        public void setAngle(float angle) {
            setStatorAngle(angle);
        }
        public void setVelocity(float velo) {
            mStator.TargetVelocityRad = velo;
            mReflection.TargetVelocityRad = -velo;
            setTorque(velo);
        }
        void setStatorAngle(float aAngle) {
            aAngle -= mStator.Angle;
            MathHelper.LimitRadians(ref aAngle);
            if (aAngle > MathHelper.Pi) {
                aAngle -= MathHelper.TwoPi;
            }
            mStator.TargetVelocityRad = aAngle;
            mReflection.TargetVelocityRad = -aAngle;
            setTorque(aAngle);
        }
    }
}
