using Library;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        const double MAX_VELO = 100.0;
        const double MIN_VELO = 0.25;
        void rotate2vector(Vector3D aTarget) {
            if (Vector3D.Zero == aTarget) {
                mGyro.GyroOverride = false;
            } else {
                mGyro.GyroOverride = true;
                pitch2vector(aTarget);
                roll2vector(aTarget);
            }
        }
        double pitch2vector(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Pitch", aTarget, m.Translation, m.Right, m.Up, m.Down
            );
        }
        double roll2vector(Vector3D aTarget) {
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
            //log("rotate2direction");
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
            //log("angleBetween ", result);
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
            setMissionObjective(mRC.WorldMatrix.Translation);
        }
        void missionDamp() => missionDamp(momentum().Length());
        void missionDamp(double aMomentumLength) {
            ThrustVector(false, false);
            return;
            if (0.01 > mdLinearVelocity) {
                log("damp to vec 0");
                rotate2vector(Vector3D.Zero);
                ThrustN(0);
            } else {
                var vRetrogradeDisplacement = mRC.WorldMatrix.Translation - mvLinearVelocity;
                var vRetrogradeDirection = Vector3D.Normalize(vRetrogradeDisplacement);
                log("damp to vec", vRetrogradeDisplacement);
                rotate2vector(vRetrogradeDisplacement);
                ThrustN(aMomentumLength * thrustPercent(mvLinearVelocityDirection, mRC.WorldMatrix.Down));
            }
        }
        void setMissionNavigate(string aWaypointName) {
            initMission();            
            MyWaypointInfo waypoint;
            if (findWaypoint(aWaypointName, out waypoint)) {
                meMission = Missions.navigate;
                setMissionObjective(waypoint.Coords);
            }
        }
        bool findWaypoint(string aName, out MyWaypointInfo aWaypoint) {
            var list = new List<MyWaypointInfo>();
            mRC.GetWaypointInfo(list);

            foreach (var p in list) {
                if (p.Name.ToLower() == aName.ToLower()) {
                    aWaypoint = p;
                    return true;
                }
            }
            aWaypoint = MyWaypointInfo.Empty;
            return false;
        }
        void setMissionPatrol() {
            initMission();
            meMission = Missions.patrol;

        }
        void setMissionObjective(Vector3D aObjective) {
            mvMissionObjective = aObjective;
            mdMissionDistance = (mvMissionObjective - mvMissionStart).Length();
        }
        void initMission() {
            mvMissionStart = mRC.WorldMatrix.Translation;

            mdMissionDistance = 
            mdMissionAltitude =
            miMissionStep = 0;

            meMission = Missions.damp;
            
            mvMissionObjective = 
            mvMissionDirection = Vector3D.Zero;

            mGyro.Enabled = true;

            mMissionConnector = null;
            mCon.Enabled = false;
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
        void doMission() {
            log("doMission ", meMission);
            switch (meMission) {
                case Missions.damp: {                    
                    if (initMissionAltitude()) {
                        var dAltitudeDifference = mdMissionAltitude - mdAltitude;
                        if (0.0 < dAltitudeDifference) {
                            ThrustVector(false, false);
                        } else {
                            missionDamp();
                        }
                    } else {
                        missionDamp();
                    }
                } break;
                case Missions.navigate:
                    missionNavigate();
                    break;
                case Missions.dock:
                    missionDock();
                    break;
                case Missions.patrol:
                    if (0 == miMissionStep) {
                        setMissionObjective(PATROL_0);
                    } else {
                        setMissionObjective(PATROL_1);
                    }
                    if (mdDistance2Objective < 10.0) {
                        miMissionStep++;
                    }
                    if (miMissionStep > 1) {
                        miMissionStep = 0;
                    }
                    missionNavigate();
                    break;
                default:
                    log("mission unhandled");
                    break;
            }
        }
        void setMissionDock(string aConnector) {
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
                var approachPlane = mMissionConnector.Position + (mMissionConnector.Direction * 1000.0);
                mMissionConnector.ApproachFinal = mMissionConnector.Position + (mMissionConnector.Direction * 500.0);
                
                var projectedPosition = project(mRC.WorldMatrix.Translation, approachPlane, mMissionConnector.Direction);
                var projectedDirection = Vector3D.Normalize(projectedPosition - approachPlane);
                mMissionConnector.Approach = approachPlane + (projectedDirection * 1000.0);
                // todo double check this
                
                Me.CustomData =
                    "Position" + Environment.NewLine + mMissionConnector.Position + Environment.NewLine +
                    "Approach" + Environment.NewLine + mMissionConnector.Approach + Environment.NewLine +
                    "Final Approach" + Environment.NewLine + mMissionConnector.ApproachFinal;
            }
        }
        void missionDock() {
            log("Dock Mission: ", mMissionConnector.Name + " - " + mMissionConnector.Id);
            var d = 0.0;
            var msg = "unknown";
            var step = (DockStep)miMissionStep;
            switch (step) {
                case DockStep.departFinal:
                case DockStep.approach:
                    setMissionObjective(mMissionConnector.Approach);
                    if (DockStep.departFinal == step) {
                        msg = "depart approach area";
                    } else {
                        msg = "rendezvous with approach";
                    }
                    // goto initial approach
                    d = distance2objective();
                    if (250.0 > d) {
                        miMissionStep++;
                    } else {
                        ThrustVector(false, false);
                    }
                    break;
                case DockStep.approachFinal:
                case DockStep.depart:
                    setMissionObjective(mMissionConnector.ApproachFinal);
                    if (DockStep.depart == step) {
                        msg = "depart dock area";
                    } else {
                        msg = "rendezvous with final approach";
                    }
                    // goto beginning of final approach
                    d = distance2objective();
                    if (10.0 > d) {
                        miMissionStep++;
                    } else {
                        ThrustVector(false, false);
                    }
                    break;
                case DockStep.dock:
                    setMissionObjective(mMissionConnector.Position);
                    msg = "zzrendezvous with dock";
                    // on final approach
                    d = distance2objective();
                    if (d < 5.0) {
                        mCon.Enabled = true;                                                
                        miMissionStep++;
                    } else if (d < 100.0) {
                        ThrustVector(false, true);
                    } else {// if slow ok not working CoM is alternative
                        ThrustVector(false, true);
                    }
                    break;
                case DockStep.connect:
                    msg = "connecting to dock";
                    rotate2vector(mvMissionObjective);
                    ThrustN(0);
                    if (mvAngularVelocity.LengthSquared() == 0 && mvLinearVelocity.LengthSquared() == 0) {
                        if (mCon.Status.HasFlag(MyShipConnectorStatus.Connectable)) {
                            initMass();
                            mCon.Connect();
                        } else if (mCon.Status.HasFlag(MyShipConnectorStatus.Connected)) {
                            miMissionStep++;
                        }
                    } else {
                        msg = "stabilizing with dock";
                    }
                    break;
                case DockStep.wait:
                    msg = "connected to dock";
                    mGyro.Enabled = false;
                    //mCon.Enabled = false;
                    if (mCon.Status != MyShipConnectorStatus.Connected) {
                        mGyro.Enabled = true;
                        mCon.Enabled = false;
                        miMissionStep++;
                    }
                    break;
                case DockStep.complete:
                    msg = "depature complete";
                    iDock++;
                    if (iDock == 3) {
                        iDock = 0;
                    }
                    setMissionDock("con" + iDock);
                    break;
                default:
                    log("step unhandled, damping");
                    missionDamp();
                    break;
            }
            log(msg);
        }
        void missionNavigate() {
            ThrustVector(false, false);
        }
        double distance2objective() => _distance2(mvMissionObjective, mRC.WorldMatrix.Translation);
        double _distance2(Vector3D aTarget, Vector3D aOrigin) => (aTarget - aOrigin).Length();
        void ThrustVector(/*double aVelocity = double.MaxValue,*/ bool aGyroHold, bool aSlowOkay) {
            // whip says
            // then using that time to intercept (tti) you propogate the state of the target forward: 
            // predictedTargetPos = currentTargetPos + currentTargetVel * tti + 0.5 * tti * tti * currentTargetAcc
            Vector3D vGravityDisplacement = mRC.GetNaturalGravity();
            var vDesiredDisplacement = mvMissionObjective - mRC.WorldMatrix.Translation;
            var distance = vDesiredDisplacement.Length();
            var vDesiredDirection = Vector3D.Normalize(vDesiredDisplacement);
            var aVelocity = mdPreferredVelocity;
            if (aSlowOkay && aVelocity < mdLinearVelocity) {
                aVelocity = mdLinearVelocity;
            }
            var vDesiredVelocity = aVelocity * vDesiredDirection;
            var vForceDisplacement = (vDesiredVelocity - mvLinearVelocity - vGravityDisplacement) * mdMass;
            var dForce = vForceDisplacement.Length();
            var vImpulseDirection = vForceDisplacement / dForce;
            var dThrustPercent = thrustPercent(vImpulseDirection, mRC.WorldMatrix.Up);
            ThrustN(dForce * dThrustPercent);

            if (aGyroHold) {
                rotate2vector(Vector3D.Zero);
            } else {
                rotate2vector(mRC.WorldMatrix.Translation + vForceDisplacement);
            }
        }
        double thrustPercent(Vector3D aDirection, Vector3D aNormal) {
            var result = 0.0;
            var offset = angleBetween(aDirection, aNormal);
            var d = 2.0;
            if (offset < Math.PI / d) {
                result = 1.0 - (offset / (Math.PI / d));
            }
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
            var vGravityDisplacement = mRC.GetNaturalGravity();
            Vector3D result = mdMass * (vGravityDisplacement + mvLinearVelocity);
            return result;
        }
        void zupdate() {
            
            /*return;
            var rcMatrix = mRC.WorldMatrix;
            var gyroMatrix = mGyro.WorldMatrix;
            // 1 N = 1 kgm/s2
            var g = mRC.GetNaturalGravity();
            
            var vGravityDisplacement = mRC.GetNaturalGravity();
            var vGravityDirection = Vector3D.Normalize(vGravityDisplacement);
            
            var vProgradeDisplacement = rcMatrix.Translation + mvLinearVelocity;
            var vRetrogradeDisplacement = rcMatrix.Translation - mvLinearVelocity;
            //var vVelocityDirection = Vector3D.Normalize(sv.LinearVelocity);
            
            
            // vec = desired - act
            // actual
            

            // desired
            var vDesiredDisplacement = BASE_SPACE_1 - mRC.WorldMatrix.Translation;
            var vDesiredDirection = Vector3D.Normalize(vDesiredDisplacement);

            // answer is the target vector
            var vPredictedDisplacement = vDesiredDirection - vForceGV;
            var vPredictedDirection = Vector3D.Normalize(vPredictedDisplacement);
            //var vProjectedDirection = project();
            */
            //rotate2vector(BASE_SPACE_1);
            //rotate2target(BASE_SPACE_1);
            //pointRotoAtTarget(get("roto0") as IMyMotorStator, BASE_SPACE_1);
            //pointRotoAtTarget(get("roto1") as IMyMotorStator, BASE_SPACE_1);
            //pointRotoAtTarget(get("roto2") as IMyMotorStator, BASE_SPACE_1);
            //pointRotoAtTarget(get("roto3") as IMyMotorStator, BASE_SPACE_1);
            //doGyro(vDesiredDirection);

            //var dThrustPercent = thrustPercent(vDesiredDirection, rcMatrix.Up);
            //log("dThrustPercent", dThrustPercent);
            //log("offset up", angleBetween(Vector3D.Normalize(vRetrogradeDisplacement), rcMatrix.Up));
            //ThrustN(vForceGV.Length() * dThrustPercent);
            
            //doGyro(vGravityDirection * -1.0);
            //log("update complete");
        }
        
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            init();
            setMissionDamp();
            mListener = IGC.RegisterBroadcastListener("docks");
            mListener.SetMessageCallback("docks");
        }
        /// <summary>
        /// Organize blocks into case insensitive dictionary
        /// </summary>
        void initBlocks() {
            
        }


        
        void init() {
            mGTS = new GTS(this);
            initLog();
            initBlocks();
            mRC = mGTS.get<IMyRemoteControl>("rc");
            mLCD = mGTS.get<IMyTextPanel>("lcd");
            mGTS.initBlockList<IMyThrust>("thrust", out marThrust);
            ThrustN(0);
            initMass();
            mGyro = mGTS.get<IMyGyro>("gyro");
            mCon = mGTS.get<IMyShipConnector>("con");
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
        void initLog() {
            if (null == mLog) {
                mLog = new StringBuilder();
            }
        }
        void initAltitude() {
            if (mRC.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out mdAltitude)) {
                log("altitude ", mdAltitude);
            } else {
                mdAltitude = 0.0;
            }
        }
        /// <summary>
        /// 1 N = 0.10197 kg × 9.80665
        /// mass = N / accel 
        /// stop distance = (velocity^2)/(2* acceleration)
        /// </summary>
        void initMass() {
            var sm = mRC.CalculateShipMass();
            
            mdMass = 0;
            
            absMax(sm.BaseMass, ref mdMass);
            absMax(sm.PhysicalMass, ref mdMass);
            absMax(sm.TotalMass, ref mdMass);
            mdAcceleration = mdNewtons / mdMass;

            // stop distance 50 = (10 * 10)/(2 * 1)
            // 0.5 = 1 / 2
        }
        void initVelocity() {
            var sv = mRC.GetShipVelocities();

            mvLinearVelocity = sv.LinearVelocity;
            mdLinearVelocity = mvLinearVelocity.Length();

            mvAngularVelocity = sv.AngularVelocity;
            mdAngularVelocity = mvAngularVelocity.Length();
            
            mvLinearVelocityDirection = mvLinearVelocity / mdLinearVelocity;
            mdStopDistance = (mdLinearVelocity * mdLinearVelocity) / (mdAcceleration * 2);
            mdStopDistance *= 10.0;

            if (mdStopDistance < 1.0) {
                mdStopDistance = 1.0;
            }
            // d           = (1                * 1               ) / (2              * 2)
            // d = 0.25
            // veloSquared = d * (accel * 2)
            // velo = sqrt(veloSquared)

            
            mdDistance2Objective = (mvMissionObjective - mRC.WorldMatrix.Translation).Length();
            var distanceFromStart = (mvMissionStart - mRC.WorldMatrix.Translation).Length();
            double dist2use = mdDistance2Objective;
            double speedfactor = 0.1;
            if (mdDistance2Objective > distanceFromStart) {
                dist2use = distanceFromStart;
            }
            log("dist2use ", dist2use);
            mdPreferredVelocity = dist2use * speedfactor;
            if (mdPreferredVelocity < MIN_VELO) {
                mdPreferredVelocity = MIN_VELO;
            } else if (mdPreferredVelocity > MAX_VELO) {
                mdPreferredVelocity = MAX_VELO;
            }
            if (mdDistance2Objective < mdPreferredVelocity) {
                mdPreferredVelocity = mdDistance2Objective;
            }
            log("Distance to Objective ", mdDistance2Objective);
            log("Distance from Start ", distanceFromStart);
            //log("acceleration ", mdAcceleration);
            log("linear velocity ", mdLinearVelocity);
            //log("angular velocity ", mdAngularVelocity);
            log("stop distance ", mdStopDistance);
            log("perferred velocity ", mdPreferredVelocity);
        }

        void receiveMessage() {
            while (mListener.HasPendingMessage) {
                var msg = mListener.AcceptMessage();
                switch (msg.Tag) {
                    case "docks":
                        Connector.FromCollection((ImmutableArray<MyTuple<long, string, Vector3D, Vector3D>>)msg.Data, mDocks);
                        break;
                }
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
                            case "patrol":
                                setMissionPatrol();
                                break;
                            case "navigate":
                                setMissionNavigate(args[1]); // GPS: MOON_DOCK
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
                    try {
                        foreach (var c in mDocks) {
                            log(c.Value.Name);
                        }
                        initAltitude();
                        initVelocity();
                        doMission();
                        str = mLog.ToString();
                        mLCD.WriteText(str);
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
        void ThrustN(double aNewtons) => ThrustN((float)aNewtons);
        void ThrustN(float aNewtons) {
            float fMax, fPercent;
            mdNewtons = 0;
            log("ThrustN requested ", aNewtons, "N");
            for (int i = 0; i < marThrust.Length; i++) {
                
                var t = marThrust[i];
                fMax = t.MaxEffectiveThrust;
                mdNewtons += fMax;
                if (aNewtons > 0.0f) {
                    
                    if (aNewtons > fMax) {
                        fPercent = 1.0f;
                        aNewtons -= fMax;
                    } else {
                        fPercent = aNewtons / fMax;
                        aNewtons = 0.0f;
                    }
                    log("Thruster #", i, " at ", 100.0 * fPercent, "%");
                    t.Enabled = true;
                    t.ThrustOverridePercentage = fPercent;
                } else {
                    log("Thruster #", i, " disabled");
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
                        targetAngle = (Math.PI * 2) - angle;
                    } else {
                        targetAngle = angle;
                    }
                    v = targetAngle - aRoto.Angle;
                    if (v > Math.PI) {
                        v -= (Math.PI * 2);
                    }
                    if (v < -Math.PI) {
                        v += (Math.PI * 2);
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
                aRoto.TargetVelocityRad = (float)(v * 6.0);
            }
        }

        public void log(double d) => log();
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
                    } else if (arg is double) {
                        mLog.Append(((double)arg).ToString("N"));
                    } else {
                        mLog.Append(arg.ToString());
                    }
                }
            }
            mLog.AppendLine();
        }

        Vector3D local2pos(Vector3D local, MatrixD world) =>
            Vector3D.Transform(local, world);
        Vector3D local2dir(Vector3D local, MatrixD world) =>
            Vector3D.TransformNormal(local, world);
        Vector3D world2pos(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world - local.Translation, MatrixD.Transpose(local));
        Vector3D world2dir(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world, MatrixD.Transpose(local));
        enum Missions
        {
            damp,
            navigate,
            dock,
            patrol
        }
        enum DampStep : int
        {
            stop,
            hold
        }
        enum DockStep : int
        {
            approach = 0,
            approachFinal,
            dock,
            connect,
            wait,
            depart,
            departFinal,
            complete
        }
        Connector mMissionConnector; 
        
        Dictionary<long, Connector> mDocks = new Dictionary<long, Connector>();
        

        double mdAltitude;
        double mdLinearVelocity = 0.0;
        double mdAngularVelocity = 0.0;
        double mdPreferredVelocity = 0.0;
        double mdMass;
        double mdAcceleration;
        double mdStopDistance;
        double mdDistance2Objective;
        double mdNewtons;
        double mdMissionAltitude = 0;
        const double mdRotateEpsilon = 0.001;
        double mdMissionDistance = 0.0;

        GTS mGTS;

        IMyThrust[] marThrust;
        IMyTextPanel mLCD;
        readonly IMyBroadcastListener mListener;
        IMyRemoteControl mRC;
        IMyGyro mGyro;
        IMyShipConnector mCon;

        int iDock = 0;
        int mCount = 0;
        int miMissionStep = 0;

        Missions meMission = Missions.damp;

        StringBuilder mLog;
        Vector3D PATROL_0 = new Vector3D(45519.94, 164664.93, -85803.92);
        Vector3D PATROL_1 = new Vector3D(46015.06, 164066.36, -86526.14);
        Vector3D MOON_DOCK = new Vector3D(19706.77, 143964.78, -109088.83);

        Vector3D pos = Vector3D.Zero;
        Vector3D mvMissionObjective;
        Vector3D mvMissionStart;
        Vector3D mvMissionDirection;

        
        Vector3D mvLinearVelocity = Vector3D.Zero;
        Vector3D mvAngularVelocity = Vector3D.Zero;
        Vector3D mvLinearVelocityDirection = Vector3D.Zero;
    }
    // large connectors distance apart 2.65 
    // small connector distance from large 1.85
    // small connector distance from small 1.00

    /*
        Vector3D mvMissionDirection;
        Vector3D otherside = new Vector3D(986148.14, 102603.57, 1599688.09);
        Vector3D BASE_HIGHER = new Vector3D(1045917.97, 142402.91, 1571139.78);
        Vector3D BASE_SPACE_1 = new Vector3D(44710.14, 164718.97, -85304.59);
        Vector3D BASE_SPACE_2 = new Vector3D(44282.68, 164548.94, -85064.41);
        Vector3D BASE_SPACE_3 = new Vector3D(44496.03, 164633.07, -85185.32);
      
    */
}
