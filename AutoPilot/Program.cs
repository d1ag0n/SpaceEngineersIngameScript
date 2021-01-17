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
            damp,
            navigate,
            dock
        }
        Vector3D mvMissionObjective;
        Vector3D mvMissionDirection;
        Connector mMissionConnector;
        Missions meMission = Missions.damp;
        double mdMissionAltitude = 0;
        int miMissionStep = 0;
        //string msMissionTag = string.Empty;
        //double mdAngularVeloPitchMax = 0.0; // local x
        //double mdAngularVeloYawMax = 0.0; // local y
        //double mdAngularVeloRollMax = 0.0; // local z
        //double mdAngularVeloPredictedYaw;
        //double mdAngularVeloPredictedPitch;
        //double mdAngularVeloPredictedRoll;
        const float mfRPM = 30.0f;
        const double mdRPM = mfRPM;
        const double mdRotateEpsilon = 0.001;

        void rotate2vector(Vector3D aTarget) {
            if (Vector3D.Zero == aTarget) {
                mGyro.GyroOverride = false;
            } else {
                mGyro.GyroOverride = true;
                pitch2vector(aTarget);
                roll2vector(aTarget);
            }
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
            log("rotate2direction");
            var angle = angleBetween(aDirection, aIntersect1);
            //log(aGyroOverride, " angle ", angle);
            double rpm = 0.0;
            if (angle > mdRotateEpsilon) {
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
            var result = Math.Acos(a.Dot(b));
            log("angleBetween ", result);
            return result;
        }
        Vector3D project(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - (Vector3D.Dot(aTarget - aPlane, aNormal) * aNormal);

        
        void absMax(double a, ref double b) {
            a = Math.Abs(a);
            if (a > b) {
                b = a;
            }
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

        void setMissionDamp() {
            initMission();
            meMission = Missions.damp;
        }
        void setMissionDock(string aConnector) {
            
            Connector c = null;

            initMission();

            var keys = mDocks.Keys.ToArray();
            double distance = double.MaxValue;
            for (int i = 0; i < keys.Length; i++) {
                var val = mDocks[keys[i]];
                if (aConnector == val.Name) {
                    var d = (val.Position - mRC.WorldMatrix.Translation).LengthSquared();
                    if (d < distance) {
                        mMissionConnector = val;
                        distance = d;
                        meMission = Missions.dock;
                    }
                }
            }
            if (null != mMissionConnector) {
                mMissionConnector.FinalApproach = mMissionConnector.Position + (mMissionConnector.Direction * 500.0);
                mMissionConnector.Approach = mRC.WorldMatrix.Translation +
                    Vector3D.Normalize(mMissionConnector.FinalApproach - project(mRC.WorldMatrix.Translation, mMissionConnector.FinalApproach, mMissionConnector.Direction)) * 1000.0;
                Me.CustomData = "Approach" + Environment.NewLine + mMissionConnector.Approach + Environment.NewLine + "Final Approach" + Environment.NewLine + mMissionConnector.FinalApproach;
            }
        }
        void setMissionNavigate(Vector3D aObjective = new Vector3D()) {
            initMission();
            meMission = Missions.navigate;
            mvMissionObjective = aObjective;            
        }
        void initMission() {
            mdMissionAltitude =
            miMissionStep = 0;

            meMission = Missions.damp;
            
            mvMissionObjective = 
            mvMissionDirection = Vector3D.Zero;

            mMissionConnector = null;
        }
        bool initMissionAltitude() {
            var result = false;
            var vGravityDisplacement = mRC.GetNaturalGravity();
            if (0.0 < vGravityDisplacement.LengthSquared()) {
                // in gravity
                if (0.0 == mdMissionAltitude) {
                    // didnt set altitude
                    if (mdAltitude > 0.0) {
                        // have +altitude
                        mdMissionAltitude = mdAltitude;
                        result = true;
                    }
                } else {
                    result = true;
                }
            }
            if (result) {
                log("mission altitude ", mdMissionAltitude);
            }
            return result;
        }
        double mdAltitude;
        void initAltitude() {
            if (mRC.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out mdAltitude)) {
                log("altitude ", mdAltitude);
            } else {            
                mdAltitude = 0.0;
            }
        }
        double mdLinearVelocity = 0.0;
        Vector3D mvPosition = Vector3D.Zero;
        Vector3D mvLinearVelocity = Vector3D.Zero;
        Vector3D mvLinearVelocityDirection = Vector3D.Zero;
        
        void initVelocity() {
            //var position = mRC.CenterOfMass;
            //mvLinearVelocity = position - mvPosition;
            var sv = mRC.GetShipVelocities();
            mvLinearVelocity = sv.LinearVelocity;
            mvPosition = mRC.WorldMatrix.Translation;
            mdLinearVelocity = mvLinearVelocity.Length();
            mvLinearVelocityDirection = mvLinearVelocity / mdLinearVelocity;
            log("linear velocity", mdLinearVelocity);
        }
        void doMission() {
            log("doMission ", meMission);
            switch (meMission) {
                case Missions.damp: {                    
                    if (initMissionAltitude()) {
                        var dAltitudeDifference = mdMissionAltitude - mdAltitude;
                        if (0.0 < dAltitudeDifference) {
                            var vGravityDisplacement = mRC.GetNaturalGravity();
                            var vGravityNormal = Vector3D.Normalize(vGravityDisplacement);
                            var target = mRC.WorldMatrix.Translation + (vGravityNormal * -dAltitudeDifference);
                            thrustVector(target, dAltitudeDifference * 0.1);
                        } else {
                            missionDamp();
                        }
                    } else {
                        missionDamp();
                    }
                } break;
                case Missions.navigate: missionNavigate();  break;
                case Missions.dock:
                    break;
                default: log("mission unhandled");          break;
            }
        }
        void missionDock() {
            
        }
        void missionDamp() => missionDamp(momentum().Length());
        void missionDamp(double aMomentumLength) {
            if (0.001 > mdLinearVelocity) {
                log("damp to vec 0");
                aMomentumLength = 0.0;
                rotate2vector(Vector3D.Zero);
            } else {
                var vRetrogradeDisplacement = mvPosition - mvLinearVelocity;
                var vRetrogradeDirection = Vector3D.Normalize(vRetrogradeDisplacement);
                log("damp to vec", vRetrogradeDisplacement);
                rotate2vector(vRetrogradeDisplacement);
            }            
            thrust(thrust0, aMomentumLength * thrustPercent(mvLinearVelocityDirection, mRC.WorldMatrix.Down));
        }
        void missionNavigate() {
            switch (miMissionStep) {
                case 0:
                    if (140.0 < (mvMissionObjective - mRC.WorldMatrix.Translation).LengthSquared()) {
                        thrustVector(mvMissionObjective);
                        log("navigating");
                    } else {
                        log("close enough");
                        if (mvMissionObjective == CONNECTOR) {
                            mvMissionObjective = CONNECTOR_APPROACH;
                        } else {
                            mvMissionObjective = CONNECTOR;
                        }
                        missionDamp();
                    }
                    break;
            }
        }
        void thrustVector(Vector3D aTarget, double aVelocity = double.MaxValue) {
            var sm = mRC.CalculateShipMass();
            
            //Vector3D vLinearVelocity = sv.LinearVelocity;
            Vector3D vGravityDisplacement = mRC.GetNaturalGravity();            
            double dMass = sm.TotalMass;
            // vec = desired - act
            var vDesiredDisplacement = aTarget - mRC.WorldMatrix.Translation;
            
            if (double.MaxValue == aVelocity) {
                aVelocity = vDesiredDisplacement.Length() * 0.1;
                if (100.0 < aVelocity) {
                    aVelocity = 100.1;
                } else if (0.0 > aVelocity) {
                    aVelocity = 0.0;
                }
            }
            var vDesiredDirection = Vector3D.Normalize(vDesiredDisplacement);
            var vDesiredVelocity = aVelocity * vDesiredDirection;
            var vForceDisplacement = (vDesiredVelocity - mvLinearVelocity - vGravityDisplacement) * dMass;
            var dForce = vForceDisplacement.Length();
            var vImpulseDirection = vForceDisplacement / dForce;
            rotate2vector(mRC.WorldMatrix.Translation + vForceDisplacement);
            var dThrustPercent = thrustPercent(vImpulseDirection, mRC.WorldMatrix.Up);
            thrust(thrust0, dForce * dThrustPercent);
        }
        Vector3D normalize(Vector3D v) {
            var len = v.Length();
            return len < 0.001 ? Vector3D.Zero : v / len;
        }
        double thrustPercent(Vector3D aDirection, Vector3D aNormal) {
            log("thrustPercent");
            var result = 0.0;
            var offset = angleBetween(aDirection, aNormal);
            var d = 2.0;
            if (offset < Math.PI / d) {
                result = 1.0 - (offset / (Math.PI / d));
            }
            log("thrustPercent ", result);
            return result;
        }
        bool missionStop() {
            var result = false;
            var dMomentum = momentum().Length();
            if (0.0 != dMomentum) {
                missionDamp(dMomentum);
            } else {
                result = true;
            }
            return result;
        }

        Vector3D momentum() {
            var sm = mRC.CalculateShipMass();
            var vGravityDisplacement = mRC.GetNaturalGravity();
            Vector3D result = sm.TotalMass * (vGravityDisplacement + mvLinearVelocity);
            log("momentum", result);
            return result;
        }


        void update() {
            log("CoM", mRC.CenterOfMass);
            doMission();
            return;
            var rcMatrix = mRC.WorldMatrix;
            var gyroMatrix = mGyro.WorldMatrix;
            // 1 N = 1 kgm/s2
            var g = mRC.GetNaturalGravity();
            var sm = mRC.CalculateShipMass();
            var vGravityDisplacement = mRC.GetNaturalGravity();
            var vGravityDirection = Vector3D.Normalize(vGravityDisplacement);
            var fMass = sm.TotalMass;
            var vProgradeDisplacement = rcMatrix.Translation + mvLinearVelocity;
            var vRetrogradeDisplacement = rcMatrix.Translation - mvLinearVelocity;
            //var vVelocityDirection = Vector3D.Normalize(sv.LinearVelocity);
            
            var vMom = fMass * mvLinearVelocity;
            // vec = desired - act
            // actual
            var vForceGV = fMass * (vGravityDisplacement + mvLinearVelocity);
            var vForceG = fMass * vGravityDisplacement;
            var vForceV = fMass * mvLinearVelocity;

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

        //Vector3D BASE_ABOVE = new Vector3D(1045810.57, 142332.61, 1571519.87);
        Vector3D BASE_HIGHER = new Vector3D(1045917.97, 142402.91, 1571139.78);
        Vector3D BASE_SPACE_1 = new Vector3D(44710.14, 164718.97, -85304.59);
        Vector3D BASE_SPACE_2 = new Vector3D(44282.68, 164548.94, -85064.41);
        Vector3D BASE_SPACE_3 = new Vector3D(44496.03, 164633.07, -85185.32);
        Vector3D CONNECTOR_APPROACH = new Vector3D(44676.88, 164938.5, -85394.41);
        Vector3D CONNECTOR = new Vector3D(44698.67, 164745.01, -85320.02);
        
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            maxV /= cf;
            mRC = get("rc") as IMyRemoteControl;
            lcd = get("lcd") as IMyTextPanel;
            init();
            setMissionNavigate(CONNECTOR);
            //setMissionDamp();
            mListener = IGC.RegisterBroadcastListener("docks");
            mListener.SetMessageCallback("docks");
        }
        void receiveMessage() {
            while (mListener.HasPendingMessage) {
                var msg = mListener.AcceptMessage();
                switch (msg.Tag) {
                    case "docks":
                        Connector.ParseAll(msg.Data.ToString(), mDocks);
                        break;
                }
            }
        }
        
        
        class Connector
        {
            public long EntityId;
            public string Name;
            public Vector3D Position;
            public Vector3D Approach;
            public Vector3D FinalApproach;
            public Vector3D Direction;
            public static void ParseAll(string aData, Dictionary<long, Connector> aDictionary) {
                var rows = aData.Split(mRowSep);
                
                for (int i = 0; i < rows.Length; i++) {
                    var c = Parse(rows[i]);
                    aDictionary[c.EntityId] = c;
                }

            }
            public static Connector Parse(string aData) {
                var result = new Connector();
                var col = aData.Split(mColSep);
                result.EntityId = long.Parse(col[0]);
                result.Name = col[1];
                Vector3D.TryParse(col[2], out result.Position);
                Vector3D.TryParse(col[3], out result.Direction);
                return result;
            }
            override public string ToString() => Name;
        }
        StringBuilder mLog;
        void initLog() {
            if (null == mLog) {
                mLog = new StringBuilder();
            }
        }
        void Main(string argument, UpdateType aUpdate) {
            string str;
            if (aUpdate.HasFlag(UpdateType.Terminal)) {
                if (null != argument) {
                    var args = argument.Split(' ');
                    if (0 < args.Length) {

                        switch (args[0]) {
                            case "dock":
                                if (1 < args.Length) {
                                    setMissionDock(args[1]);
                                }
                                break;
                            case "damp":
                                setMissionDamp();
                                break;
                        }
                    }
                }
            }
            if (aUpdate.HasFlag(UpdateType.IGC)) {
                receiveMessage();
            }
            if (aUpdate.HasFlag(UpdateType.Update1)) {
                mCount++;
                if (10 == mCount) {
                    mCount = 0;
                    initLog();
                    mLog = new StringBuilder();
                    try {
                        initAltitude();
                        initVelocity();
                        update();
                        str = mLog.ToString();
                        lcd.WriteText(str);
                    } catch (Exception ex) {
                        log(ex);
                        str = mLog.ToString();
                    }
                    mLog = null;
                    Echo(str);
                }
            }
            mLog = null;
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
        //const float fMagik = 3.0f;

        Vector3D drone = new Vector3D(992497.0, 98921.0, 1668849.0);
        Vector3D droneHigher = new Vector3D(992136.99, 98604.87, 1669245.49);
        //Vector3D target = new Vector3D(992136.99, 98604.87, 1669245.49);
        //Vector3D target = new Vector3D(1032768.24, 138549.56, 1568429.17);
        //Vector3D target = new Vector3D(1033485.69, 154992.3, 1504229.77);
        //Vector3D tango = new Vector3D(1033485.69, 154992.3, 1504229.77);
        Vector3D otherside = new Vector3D(986148.14, 102603.57, 1599688.09);
        IMyThrust thrust0;
        int mCount = 0;
        IMyTextPanel lcd;
        IMyCockpit pit;
        
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

        public void log(Vector3D v) => log("X ", v.X, null, "Y ", v.Y, null, "Z ", v.Z);
        public void log(params object[] args) {
            if (null != args) {
                for (int i = 0; i < args.Length; i++) {
                    var arg = args[i];
                    if (null == arg) {
                        mLog.AppendLine();
                    } else if (arg is Vector3D) {
                        mLog.AppendLine();
                        log((Vector3D)arg);
                    } else {
                        mLog.Append(arg.ToString());
                    }
                }
            }
            mLog.AppendLine();
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
            if (angle > mdRotateEpsilon) {
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
            if (angle > mdRotateEpsilon) {
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
            if (angle > mdRotateEpsilon) {
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
            if (angle > mdRotateEpsilon) {
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
        IMyBroadcastListener mListener;
        const char mRowSep = '@';
        const char mColSep = '!';
        Dictionary<long, Connector> mDocks = new Dictionary<long, Connector>();
    }
}
