using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        enum Missions
        {
            idle,
            damp
        }
        Missions eMission = Missions.damp;
        double dAngularVeloPitchMax = 0.0; // local x
        double dAngularVeloYawMax = 0.0; // local y
        double dAngularVeloRollMax = 0.0; // local z
        double dAngularVeloPredictedYaw;
        double dAngularVeloPredictedPitch;
        double dAngularVeloPredictedRoll;
        const float fRPM = 30.0f;
        const double dRPM = fRPM;
        const double dRotateEpsilon = 0.001;

        void rotate2vector(Vector3D aTarget) {
            pitch2vector(aTarget);
            roll2vector(aTarget);
        }

        void rotate2target(Vector3D aTarget) {
            pitch2target(aTarget);
            yaw2target(aTarget);
            roll2target(aTarget);
        }
        double yaw2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Yaw", aTarget, m.Translation, m.Up, m.Forward, m.Forward
            );
        }
        double pitch2vector(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Pitch", aTarget, m.Translation, m.Right, m.Up, m.Down
            );
        }
        double pitch2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Pitch", aTarget, m.Translation, m.Right, m.Forward, m.Backward
            );
        }
        double roll2vector(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Roll", aTarget, m.Translation, m.Forward, m.Up, m.Down
            );
        }
        double roll2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Roll", aTarget, m.Translation, m.Forward, m.Up, m.Down
            );
        }
        double rotate2target(string aGyroOverride, Vector3D aTarget, Vector3D aPlane, Vector3D aNormal, Vector3D aIntersect1, Vector3D aIntersect2) {
            // yaw
            // m.Translation = aPlane
            // m.Up = aNormal
            // m.Forward = aIntersect
            var position = project(aTarget, aPlane, aNormal);
            var displacement = position - aPlane;
            var direction = Vector3D.Normalize(displacement);
            return rotate2direction(aGyroOverride, direction, aNormal, aIntersect1, aIntersect2);
        }
        double rotate2direction(string aGyroOverride, Vector3D aDirection, Vector3D aNormal, Vector3D aIntersect1, Vector3D aIntersect2) {
            var angle = angleBetween(aDirection, aIntersect1);
            //log(aGyroOverride, " angle ", angle);
            double rpm = 0.0;
            if (angle > dRotateEpsilon) {
                var norm = Vector3D.Normalize(aDirection.Cross(aIntersect2));
                var dot = aNormal.Dot(norm);
                if (dot < 0) {
                    angle = -angle;
                }
                rpm = rps2rpm(angle);
            }
            mGyro.SetValueFloat(aGyroOverride.ToString(), (float)rpm);
            //log(aGyroOverride, " rpm ", rpm);
            return rpm;
        }

        double rps2rpm(double rps) => (rps / (Math.PI * 2)) * 60.0;
        double rpm2rps(double rpm) => (rpm * (Math.PI * 2)) / 60.0;

        double angleBetween(Vector3D a, Vector3D b) {
            //log("angle alen ", a.Length(), " blen ", b.Length());
            return Math.Acos(a.Dot(b));
        }
        Vector3D project(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - (Vector3D.Dot(aTarget - aPlane, aNormal) * aNormal);

        Vector3D vAngularVelocity;
        void absMax(double a, ref double b) {
            a = Math.Abs(a);
            if (a > b) {
                b = a;
            }
        }
        void doGyro(Vector3D worldNormalDirection) {
            Matrix m;


            var angle = Math.Acos(Vector3D.Normalize(worldNormalDirection).Dot(mRC.WorldMatrix.Up));
            log("angle between", angle);

            // gyro prediction
            var sv = mRC.GetShipVelocities();

            //log("angular velo natural", sv.AngularVelocity);
            if (angle < 0.02) {
                log("swapped");
                //var a = BASE_SPACE_1;
                //BASE_SPACE_1 = BASE_SPACE_2;
                //BASE_SPACE_2 = a;
                mGyro.SetValueFloat("Roll", 60.0f);
                return;
            }
            if (angle > 2.5) {
                //angle = 1.0;
            } else {
                log("scaling");
                angle *= 0.7;
            }

            var vAngular = world2dir(sv.AngularVelocity, mGyro.WorldMatrix);

            absMax(vAngular.X, ref dAngularVeloPitchMax);
            absMax(vAngular.Y, ref dAngularVeloYawMax);
            absMax(vAngular.Z, ref dAngularVeloRollMax);

            var vAngularMax = new Vector3D(dAngularVeloPitchMax, dAngularVeloYawMax, dAngularVeloRollMax);
            //log("Angular Max", vAngularMax);

            vAngularVelocity = vAngular;
            // end prediction

            //log("angular velo transformed", vAngularVelocity);
            mRC.Orientation.GetMatrix(out m);
            Vector3D rcUp = m.Up;
            mGyro.Orientation.GetMatrix(out m);

            // original Vector3D gyroDwn = Vector3D.Transform(rcDown, MatrixD.Transpose(m));
            Vector3D gyroUp = Vector3D.Transform(rcUp, MatrixD.Transpose(m));

            Vector3D gyroTgt = world2dir(worldNormalDirection, mGyro.WorldMatrix);
            log("gyroTgt", gyroTgt);
            log("gyroUp", gyroUp);
            Vector3D gyroRot = Vector3D.Cross(gyroUp, gyroTgt);
            log("required rotation", gyroRot);
            var x = rot2rpm(gyroRot.X, angle);
            var y = -rot2rpm(gyroRot.Y, angle);
            var z = -rot2rpm(gyroRot.Z, angle);
            log("gyro", null, x, null, y, null, z);
            //return;


            mGyro.SetValueFloat("Pitch", (float)x);
            mGyro.SetValueFloat("Yaw", (float)y);
            mGyro.SetValueFloat("Roll", (float)z);
        }

        
        double rot2rpm(double x, double scale) {
            if (x > 1.0) {
                log("BAD ", x);
            } else if (x < -1.0) {
                log("BAD ", x);
            }
            var result = Math.Asin(x) / 2.0 / Math.PI * 60.0 / 0.166666;
            if (Math.Abs(result) < 0.001) {
                //result = 0.0;
            }
            if (result > 60.0) {
                log("dying ", result);
                //Me.Enabled = false;
            }
            return result * scale;
        }
        double doMission() {
            switch (eMission) {
                case Missions.damp: return missionDamp();
                default: return 1.0;
            }
        }
        double missionDamp() {
            var rcMatrix = mRC.WorldMatrix;
            var sv = mRC.GetShipVelocities();
            var vRetrogradeDisplacement = rcMatrix.Translation - sv.LinearVelocity;
            var vRetrogradeDirection = Vector3D.Normalize(vRetrogradeDisplacement);
            rotate2vector(vRetrogradeDisplacement);
            thrust(thrust0, momentum().Length() * thrustPercent(vRetrogradeDirection, rcMatrix.Up));            
            return 0.0;
        }
        Vector3D momentum() {
            var sm = mRC.CalculateShipMass();
            var sv = mRC.GetShipVelocities();
            var vGravityDisplacement = mRC.GetNaturalGravity();
            return sm.TotalMass * (vGravityDisplacement + sv.LinearVelocity);
        }
        double update() {
            return doMission();
            var rcMatrix = mRC.WorldMatrix;
            var gyroMatrix = mGyro.WorldMatrix;
            // 1 N = 1 kgm/s2
            var g = mRC.GetNaturalGravity();
            var sv = mRC.GetShipVelocities();
            var sm = mRC.CalculateShipMass();
            var vGravityDisplacement = mRC.GetNaturalGravity();
            var vGravityDirection = Vector3D.Normalize(vGravityDisplacement);
            var fMass = sm.TotalMass;
            var vProgradeDisplacement = rcMatrix.Translation + sv.LinearVelocity;
            var vRetrogradeDisplacement = rcMatrix.Translation - sv.LinearVelocity;
            //var vVelocityDirection = Vector3D.Normalize(sv.LinearVelocity);
            
            var vMom = fMass * sv.LinearVelocity;
            // vec = desired - act
            // actual
            var vForceGV = fMass * (vGravityDisplacement + sv.LinearVelocity);
            var vForceG = fMass * vGravityDisplacement;
            var vForceV = fMass * sv.LinearVelocity;

            // desired
            var vDesiredDisplacement = BASE_SPACE_1 - mRC.WorldMatrix.Translation;
            var vDesiredDirection = Vector3D.Normalize(vDesiredDisplacement);

            // answer is the target vector
            var vPredictedDisplacement = vDesiredDirection - vForceGV;
            var vPredictedDirection = Vector3D.Normalize(vPredictedDisplacement);
            //var vProjectedDirection = project();
            //thrust(thrust0, vForceGV.Length());
            
            rotate2vector(BASE_SPACE_1);
            //rotate2target(BASE_SPACE_1);
            //pointRotoAtTarget(get("roto0") as IMyMotorStator, BASE_SPACE_1);
            //pointRotoAtTarget(get("roto1") as IMyMotorStator, BASE_SPACE_1);
            //pointRotoAtTarget(get("roto2") as IMyMotorStator, BASE_SPACE_1);
            //pointRotoAtTarget(get("roto3") as IMyMotorStator, BASE_SPACE_1);
            //doGyro(vDesiredDirection);

            var dThrustPercent = thrustPercent(vDesiredDirection, rcMatrix.Up);
            log("dThrustPercent", dThrustPercent);
            //log("offset up", angleBetween(Vector3D.Normalize(vRetrogradeDisplacement), rcMatrix.Up));
            thrust(thrust0, vForceGV.Length() * dThrustPercent);
            //thrust(thrust0, 0.0);
            //doGyro(vGravityDirection * -1.0);
            log("update complete");
        }
        double thrustPercent(Vector3D aDirection, Vector3D aNormal) {
            var result = 0.0;
            var offset = angleBetween(aDirection, aNormal);
            if (offset < Math.PI / 2.0) {
                result = 1.0 - (offset / (Math.PI / 2.0));
            }
            return result;
        }
        //Vector3D BASE_ABOVE = new Vector3D(1045810.57, 142332.61, 1571519.87);
        Vector3D BASE_HIGHER = new Vector3D(1045917.97, 142402.91, 1571139.78);
        Vector3D BASE_SPACE_1 = new Vector3D(44710.14, 164718.97, -85304.59);
        Vector3D BASE_SPACE_2 = new Vector3D(44282.68, 164548.94, -85064.41);
        Vector3D BASE_SPACE_3 = new Vector3D(44496.03, 164633.07, -85185.32);
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            maxV /= cf;
            mRC = get("rc") as IMyRemoteControl;
            pit = get("pit") as IMyCockpit;
            lcd = get("lcd") as IMyTextPanel;
            init();
        }
        Vector3D normalise(Vector3D v) {
            var len = v.Length();
            return len < 0.001 ? Vector3D.Zero : v / len;
        }
        void Main(string argument, UpdateType aUpdate) {
            string str;

            count++;
            if (10 == count) {
                count = 0;
                sb = new StringBuilder();
                try {
                    update();
                    str = sb.ToString();
                    lcd.WriteText(str);
                } catch (Exception ex) {
                    log(ex);
                    str = sb.ToString();
                }
                Echo(str);
            }
        }
        void thrust(IMyThrust t, double f) => thrust(t, (float)f);
        void thrust(IMyThrust t, float f) {
            if (null != t) {
                float fMax = t.MaxEffectiveThrust;
                if (f > fMax) {
                    f = 1.0f;
                } else {
                    f = f / fMax;
                }
                log("thrust% ", f);
                if (f > 0.0f) {
                    t.Enabled = true;
                    t.ThrustOverridePercentage = f;
                } else {
                    t.Enabled = false;
                }
            }
        }
        void motor2Angle(IMyMotorStator aHinge, float aAngle) {
            if (check(aHinge)) {
                var delta = aAngle - aHinge.Angle;
                log("hinge delta", delta);
                aHinge.TargetVelocityRad = delta;
            }
        }
        bool check(IMyMotorStator aMotor) {
            var result =
                null != aMotor &&
                aMotor.Enabled &&
                aMotor.IsWorking &&
                !aMotor.RotorLock &&
                aMotor.IsFunctional;
            if (!result) {
                log("check failed");
            }
            return result;
        }
        void pointRotoAtTarget(IMyMotorStator aRoto, Vector3D aTarget) {
            // cos(angle) = dot(vecA, vecB) / (len(vecA)*len(vecB))
            // angle = acos(dot(vecA, vecB) / (len(vecA)*len(vecB)))
            // angle = acos(dot(vecA, vecB) / sqrt(lenSq(vecA)*lenSq(vecB)))
            if (check(aRoto)) {
                var matrix = aRoto.WorldMatrix;
                var projectedTarget = aTarget - Vector3D.Dot(aTarget - matrix.Translation, matrix.Up) * matrix.Up;
                var projectedDirection = Vector3D.Normalize(matrix.Translation - projectedTarget);
                pointRotoAtDirection(aRoto, projectedDirection);
            }
        }
        void pointRotoAtDirection(IMyMotorStator aRoto, Vector3D aDirection) {
            if (null != aRoto) {
                var matrix = aRoto.WorldMatrix;
                var angle = Math.Acos(aDirection.Dot(matrix.Forward));
                //log("roto angle ", angle);
                double targetAngle;
                var v = 0.0;
                if (!double.IsNaN(angle)) {
                    var norm = Vector3D.Normalize(aDirection.Cross(matrix.Forward));
                    var dot = matrix.Up.Dot(norm);
                    if (dot < 0) {
                        targetAngle = pi2 - angle;
                    } else {
                        targetAngle = angle;
                    }
                    v = targetAngle - aRoto.Angle;
                    if (v > pi) {
                        v -= pi2;
                    }
                    if (v < -pi) {
                        v += pi2;
                    }
                    if (v > 0) {
                        if (v < 0.01) {
                            v = 0.0;
                        }
                    } else {
                        if (v > 0.01) {
                            v = 0.0;
                        }
                    }
                } else {
                    log("angle nan");
                }
                aRoto.TargetVelocityRad = (float)(v * dMagik);
            }
        }
        const double dMagik = 3.0;
        const float fMagik = 3.0f;

        Vector3D drone = new Vector3D(992497.0, 98921.0, 1668849.0);
        Vector3D droneHigher = new Vector3D(992136.99, 98604.87, 1669245.49);
        //Vector3D target = new Vector3D(992136.99, 98604.87, 1669245.49);
        //Vector3D target = new Vector3D(1032768.24, 138549.56, 1568429.17);
        //Vector3D target = new Vector3D(1033485.69, 154992.3, 1504229.77);
        //Vector3D tango = new Vector3D(1033485.69, 154992.3, 1504229.77);
        Vector3D otherside = new Vector3D(986148.14, 102603.57, 1599688.09);
        IMyThrust thrust0;
        int count = 0;
        IMyTextPanel lcd;
        IMyCockpit pit;
        StringBuilder sb;
        void groupPropSet<T>(string group, string prop, T value) {
            var list = new List<IMyTerminalBlock>();
            //foreach (var block in GridTerminalSystem.GetBlockGroupWithName(group).
            //block.SetValue<T>(name, value);
        }

        void pointRotoAtOld(IMyMotorStator roto, Vector3D dir) {
            if (null != roto) {
                var matrix = roto.WorldMatrix;
                var va = matrix.Right;
                var vn = matrix.Up;
                double angle = Math.Acos(dir.Dot(matrix.Forward));
                double targetAngle;
                double v = 0.0;
                if (!double.IsNaN(angle)) {
                    var norm = Vector3D.Normalize(dir.Cross(matrix.Forward));
                    var dot = matrix.Up.Dot(norm);
                    if (dot > 0) {
                        targetAngle = angle;
                    } else {
                        targetAngle = pi2 - angle;
                    }
                    v = targetAngle - (float)roto.Angle;
                    if (v > pi) {
                        v -= pi2;
                    }
                    if (v < -pi) {
                        log("mod velo pos");
                        v += pi2;
                    }
                }
                roto.TargetVelocityRad = (float)(v * dMagik);
            }
        }

        void init() {
            thrust0 = get("thrust0") as IMyThrust;
            mGyro = get("gyro") as IMyGyro;
            //roto0 = get("roto0") as IMyMotorStator;
            //roto1 = get("roto1") as IMyMotorStator;
            //roto2 = get("roto2") as IMyMotorStator;
            //hinge0 = get("hinge0") as IMyMotorStator;
            //hinge1 = get("hinge1") as IMyMotorStator;
            //thrust0 = get("thrust0") as IMyThrust;

            //roto0.TargetVelocityRad = Single.MaxValue;
            //roto1.TargetVelocityRad = Single.MaxValue;
            //roto2.TargetVelocityRad = Single.MaxValue;
            //rotoVeloMax = roto0.TargetVelocityRad;
            //if (roto1.TargetVelocityRad < rotoVeloMax) rotoVeloMax = roto1.TargetVelocityRad;
            //if (roto2.TargetVelocityRad < rotoVeloMax) rotoVeloMax = roto2.TargetVelocityRad;
            /*return;
                                            roto0.TargetVelocityRad =
                                            roto1.TargetVelocityRad =
                                            roto2.TargetVelocityRad = 0.0f;
                                            roto0.Enabled =
                                            roto1.Enabled =
                                            roto2.Enabled = true;*/
        }

        void log(Vector3D v) => log("X ", v.X, null, "Y ", v.Y, null, "Z ", v.Z);
        void log(params object[] args) {
            foreach (var arg in args) {
                if (null == arg) {
                    sb.AppendLine();
                } else if (arg is Vector3D) {
                    sb.AppendLine();
                    log((Vector3D)arg);
                } else {
                    sb.Append(arg.ToString());
                }
            }
            sb.AppendLine();
        }
        object get(string n) => GridTerminalSystem.GetBlockWithName(n);

        Vector3D local2pos(Vector3D local, MatrixD world) =>
            Vector3D.Transform(local, world);
        Vector3D local2dir(Vector3D local, MatrixD world) =>
            Vector3D.TransformNormal(local, world);
        Vector3D world2pos(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world - local.Translation, MatrixD.Transpose(local));
        Vector3D world2dir(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world, MatrixD.Transpose(local));

        double pi = Math.PI;
        double pi2 = Math.PI * 2.0;
        double halfpi = Math.PI * 0.5;
        IMyRemoteControl mRC;
        double maxV = 104.4; // speed cap in m/s
        double cf = 2;// correction factor (decelleration speed)
        IMyGyro mGyro;
        Vector3D pos = Vector3D.Zero;

        // old methods
        double zrotate2target(string aDirection, Vector3D aTarget, Vector3D aPlane, Vector3D aNormal, Vector3D aIntersect1, Vector3D aIntersect2) {
            // yaw
            // m.Translation = aPlane
            // m.Up = aNormal
            // m.Forward = aIntersect
            var position = project(aTarget, aPlane, aNormal);
            var displacement = position - aPlane;
            var direction = Vector3D.Normalize(displacement);
            var angle = angleBetween(direction, aIntersect1);
            log(aDirection, " angle ", angle);
            double rpm = 0.0;
            if (angle > dRotateEpsilon) {
                var norm = Vector3D.Normalize(direction.Cross(aIntersect2));
                var dot = aNormal.Dot(norm);
                if (dot < 0) {
                    angle = -angle;
                }
                rpm = rps2rpm(angle);
            }
            mGyro.SetValueFloat(aDirection.ToString(), (float)rpm);
            log(aDirection, " rpm ", rpm);
            return rpm;
        }
        double zyaw2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            // yaw
            var position = project(aTarget, m.Translation, m.Up);
            var displacement = position - m.Translation;
            var direction = Vector3D.Normalize(displacement);
            var angle = angleBetween(direction, m.Forward);
            log("yaw angle ", angle);
            double rpm = 0.0;
            if (angle > dRotateEpsilon) {
                var norm = Vector3D.Normalize(direction.Cross(m.Forward));
                var dot = m.Up.Dot(norm);
                if (dot < 0) {
                    angle = -angle;
                }
                rpm = rps2rpm(angle);
            }
            //gyro.SetValueFloat("Yaw", (float)rpm);
            mGyro.SetValueFloat("Yaw", 0.0f);
            return rpm;
        }
        double zpitch2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            // pitch
            var position = project(aTarget, m.Translation, m.Right);
            var displacement = position - m.Translation;
            var direction = Vector3D.Normalize(displacement);
            var angle = angleBetween(direction, m.Forward);
            log("pitch angle", angle);
            double rpm = 0.0;
            if (angle > dRotateEpsilon) {
                var norm = Vector3D.Normalize(direction.Cross(m.Forward));
                var dot = m.Right.Dot(norm);
                if (dot > 0) {
                    angle = -angle;
                }
                rpm = rps2rpm(angle);
            }
            mGyro.SetValueFloat("Pitch", (float)rpm);
            return rpm;
        }
        double zroll2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            var position = project(aTarget, m.Translation, m.Forward);
            var displacement = position - m.Translation;
            var direction = Vector3D.Normalize(displacement);
            var angle = angleBetween(direction, m.Up);
            log("roll angle", angle);
            double rpm = 0.0;
            if (angle > dRotateEpsilon) {
                var norm = Vector3D.Normalize(direction.Cross(m.Up));
                var dot = m.Forward.Dot(norm);
                if (dot > 0) {
                    angle = -angle;
                }
                rpm = rps2rpm(angle);
            }
            mGyro.SetValueFloat("Roll", (float)rpm);
            //gyro.SetValueFloat("Roll", 0.0f);
            return rpm;
        }
    }
}
