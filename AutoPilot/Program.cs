using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        const double MAX_VELO = 100.0;
        const double MIN_VELO = 0.03;
        double rotate2vector(Vector3D aTarget) {
            double result = 0;
            if (Vector3D.Zero == aTarget) {
                mGyro.GyroOverride = false;
                if (mdAngularVelocity == 0) {
                    mGyro.Enabled = false;
                }
            } else {
                mGyro.GyroOverride = true;
                result += Math.Abs(pitch2vector(aTarget));
                result += Math.Abs(roll2vector(aTarget));
            }
            return result;
        }
        double yaw2target(Vector3D aTarget) {
            var m = mRC.WorldMatrix;
            return rotate2target(
                "Yaw", aTarget, (m.Translation + m.Backward), m.Up, m.Forward, m.Forward
            );
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
            var position = reject(aTarget, aPlane, aNormal);
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
            mGyro.GyroOverride = true;
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
        // orthogonal projection is vector rejection
        Vector3D reject(Vector3D aTarget, Vector3D aPlane, Vector3D aNormal) =>
            aTarget - (Vector3D.Dot(aTarget - aPlane, aNormal) * aNormal);
        void absMax(double a, ref double b) {
            a = Math.Abs(a);
            if (a > b) {
                b = a;
            }
        }
        double rot2rpm(double x, double scale) {
            if (x > 1.0) {
                log.log("BAD ", x);
            } else if (x < -1.0) {
                log.log("BAD ", x);
            }
            var result = Math.Asin(x) / 2.0 / Math.PI * 60.0 / 0.166666;
            if (Math.Abs(result) < 0.001) {
                //result = 0.0;
            }
            if (result > 60.0) {
                log.log("dying ", result);
                //Me.Enabled = false;
            }
            return result * scale;
        }
        void setMissionTrajectory(bool aThrust) {
            initMission();
            initMass();
            meMission = aThrust ? Missions.thrust : Missions.rotate;
        }
        void setMissionDamp() {
            initMission();
            meMission = Missions.damp;
            mvMissionStart = Vector3D.Zero;
            setMissionObjective(mRC.CenterOfMass);
        }
        void missionDamp() => missionDamp(momentum().Length());
        void missionDamp(double aMomentumLength) {
            if (mdDistance2Objective < 0.1) {
                rotate2vector(Vector3D.Zero);
                ThrustN(0);
            } else {
                ThrustVector(false, false);
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
            mvMissionStart = mRC.CenterOfMass;
            mdMissionDistance = 
            mdMissionAltitude =
            miMissionStep = 0;

            meMission = Missions.none;
            
            mvMissionObjective = Vector3D.Zero;

            mGyro.Enabled = true;
            mGyro.GyroOverride = false;
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
                log.log("mission altitude ", mdMissionAltitude);
            }
            return result;
        }
        void doMission() {
            log.log("doMission ", meMission);
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
                case Missions.test:
                    mGyro.SetValueFloat("Pitch", 10.0f);
                    var c = findConnector("con1");
                    var local = -world2pos(mRC.CenterOfMass, mCon.WorldMatrix);
                    //var objective = local2pos(local, c.World) + (c.World.Forward * 2.65);
                    //log.log(gps("test con1", objective));
                    break;
                default:
                    log.log("mission unhandled");
                    break;
            }
        }
        Connector findConnector(string aConnector) {
            Connector result = null;
            var keys = mDocks.Keys.ToArray();
            double distance = double.MaxValue;
            for (int i = 0; i < keys.Length; i++) {
                var val = mDocks[keys[i]];
                if (aConnector == val.Name) {
                    var d = (val.Position - mRC.CenterOfMass).LengthSquared();
                    if (d < distance) {
                        result = val;
                        distance = d;
                        
                    }
                }
            }
            return result;
        }
        void setMissionDock(string aConnector) {
            initMission();

            mMissionConnector = findConnector(aConnector);
            if (null != mMissionConnector) {
                var approachDistance = 600;
                var finalDistance = approachDistance * 0.5;
                
                meMission = Missions.dock;
                var approachPlane = mMissionConnector.Position + (mMissionConnector.Direction * approachDistance);
                mMissionConnector.ApproachFinal = mMissionConnector.Position + (mMissionConnector.Direction * finalDistance);

                var projectedPosition = reject(mRC.CenterOfMass, approachPlane, mMissionConnector.Direction);
                var projectedDirection = Vector3D.Normalize(projectedPosition - approachPlane);
                mMissionConnector.Approach = approachPlane + (projectedDirection * approachDistance);

                mMissionConnector.Objective = mMissionConnector.Position + (mMissionConnector.Direction * (mCon.WorldMatrix.Translation - mRC.CenterOfMass).Length());
            }
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
        string gps(string aName, Vector3D aPos) {
            // GPS:ARC_ABOVE:19680.65:144051.53:-109067.96:#FF75C9F1:
            var sb = new StringBuilder("GPS:");
            sb.Append(aName);
            sb.Append(":");
            sb.Append(aPos.X);
            sb.Append(":");
            sb.Append(aPos.Y);
            sb.Append(":");
            sb.Append(aPos.Z);
            sb.Append(":#FFFF00FF:");
            return sb.ToString();
        }
        void missionDock() {
            log.log("Dock Mission: ", mMissionConnector.Name);
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
                    }
                    ThrustVector(false, false);
                    break;
                case DockStep.approachFinal:
                case DockStep.depart:
                    setMissionObjective(mMissionConnector.ApproachFinal);
                    msg = "rendezvous with final approach";
                    var precision = 25.0;
                    if (DockStep.depart == step) {
                        msg = "depart dock area";
                        precision = 100.0;
                    }
                    // goto beginning of final approach
                    
                    if (precision > missionNavigate()) {
                        miMissionStep++;
                    } else {
                        ThrustVector(false, false);
                    }
                    break;
                case DockStep.dock:
                    setMissionObjective(mMissionConnector.Objective);
                    msg = "rendezvous with dock";
                    d = missionNavigate();
                    if (d == 0.0) {
                        mCon.Enabled = true;
                        miMissionStep++;
                    }
                    break;
                case DockStep.connect:
                    msg = "connecting to dock";
                    switch (mCon.Status) {
                        case MyShipConnectorStatus.Connectable:
                            rotate2vector(Vector3D.Zero);
                            ThrustN(0);
                            if (mvAngularVelocity.LengthSquared() == 0 && mdLinearVelocity == 0) {
                                initMass();
                                mCon.Connect();
                            }
                            break;
                        case MyShipConnectorStatus.Unconnected:
                            if (0 == mRC.GetNaturalGravity().LengthSquared()) {
                                if (1.0 > rotate2vector(mMissionConnector.Position + (mMissionConnector.Direction * 1000.0)) && !mMissionConnector.MessageSent) {
                                    var dockMessage = new DockMessage(mMissionConnector.Id, "align", mCon.WorldMatrix.Translation);
                                    IGC.SendUnicastMessage(mMissionConnector.ManagerId, "dockcommand", dockMessage.Data());
                                }
                                ThrustN(0);
                            } else {
                                ThrustVector(false, false);
                            }
                            break;
                        case MyShipConnectorStatus.Connected:
                            mGyro.Enabled = false;
                            miMissionStep++;
                            break;
                    }
                    break;
                case DockStep.wait:
                    msg = "connected to dock";
                    mGyro.Enabled = false;
                    mCon.Enabled = false;
                    if (mCon.Status != MyShipConnectorStatus.Connected) {
                        mGyro.Enabled = true;
                        
                        miMissionStep++;
                        mCon.Enabled = false;
                    }
                    break;
                case DockStep.complete:
                    msg = "depature complete";
                    miDock++;
                    if (miDock == 2) {
                        miDock = 0;
                    }
                    setMissionDock("con" + miDock);
                    break;
                default:
                    log.log("step unhandled, damping");
                    missionDamp();
                    break;
            }
            log.log(msg);
        }
        double missionNavigate() {
            /*
            var displacement2objective = mvMissionObjective - mvMissionStart;
            var displacement2start = mvMissionStart - mvMissionObjective;
            var missionDistance = displacement2objective.Length();
            var dir2objective = displacement2objective / missionDistance;
            var dir2start = dir2objective * -1.0;
            var dist2objectiveFromShip = (mvMissionObjective - mvCoM).Length();
            var dist2startFromShip = (mvCoM - mvMissionStart).Length();
            if (dist2objectiveFromShip > 10.0) {
                var dist2start = (mvMissionStart - mvCoM).LengthSquared();
                var missionMiddle = mvMissionStart + (displacement2objective * 0.5);
                var dir2shipFromMiddle = Vector3D.Normalize(mvCoM - missionMiddle);
                var dist2between = displacement2start.Length();
                var up = Vector3D.Normalize(mRC.GetNaturalGravity() * -1);
                var dot = up.Dot(Vector3D.Normalize(mvCoM - missionMiddle));
                // norm = dirToShipFromMiddle cross directionToObjectiveFromMiddle
                // dot = dir to obj dot norm
                // var norm = Vector3D.Normalize(aDirection.Cross(matrix.Forward));
                // var dot = matrix.Up.Dot(norm);

                var projection = project(mvCoM, missionMiddle, up);
                var distFromPlane = (mvCoM - projection).Length();
                var missionRadius = missionDistance * 0.5;
                var distance2projection = (projection - missionMiddle).Length();

                log("remainingDistance ", remainingDistance);
                log("distFromPlane ", distFromPlane);
                if (dot > 0) {
                    log("above plane");
                } else {
                    log("below plane");
                }
            }
            */
            // mvAltitudeDynamic = projection + (up * remainingDistance);
            
            var result = ThrustVector(false, false);

            if (result < 0.5) {
                ThrustN(0);
                rotate2vector(Vector3D.Zero);
                result = 0.0;
                
            }
            log.log("navigate result ", result);
            return result;

            //rotate2vector(Vector3D.Zero);

        }
        double distance2objective() => _distance2(mvMissionObjective, mRC.CenterOfMass);
        double _distance2(Vector3D aTarget, Vector3D aOrigin) => (aTarget - aOrigin).Length();
        double ThrustVector(/*double aVelocity = double.MaxValue,*/ bool aGyroHold, bool aSlowOkay) {
            return trajectory(mvMissionObjective, aGyroHold);
            // whip says
            // then using that time to intercept (tti) you propogate the state of the target forward: 
            // predictedTargetPos = currentTargetPos + currentTargetVel * tti + 0.5 * tti * tti * currentTargetAcc

            /// me says
            /// todo feed altitude different back into gravity force
            Vector3D vGravityDisplacement = mRC.GetNaturalGravity();
            /*
            if (mdAltitudeDynamic > 11.0) {
                vGravityDisplacement *= (mdAltitudeDynamic * 0.1);
            } else if (mdAltitudeDynamic > 1.0) {
                vGravityDisplacement *= mdAltitudeDynamic;
            }*/
            var objective = mvMissionObjective;
            
            var vDesiredDisplacement = objective - mRC.CenterOfMass;
            var distance = vDesiredDisplacement.Length();
            var vDesiredDirection = Vector3D.Normalize(vDesiredDisplacement);
            var aVelocity = mdPreferredVelocity;
            if (aSlowOkay && mdLinearVelocity < aVelocity) {
                aVelocity = mdLinearVelocity;
            }
            var vDesiredVelocity = aVelocity * vDesiredDirection;
            var vForceDisplacement = (vDesiredVelocity - mvLinearVelocity - vGravityDisplacement) * mdMass;
            var dForce = vForceDisplacement.Length();
            var vImpulseDirection = vForceDisplacement / dForce;
            var dThrustPercent = thrustPercent(vImpulseDirection, mRC.WorldMatrix.Up);

            ThrustN(dForce * dThrustPercent);
            //ThrustN(trajectory(false) * dThrustPercent);

            if (aGyroHold) {
                rotate2vector(Vector3D.Zero);
            } else {
                rotate2vector(objective + vForceDisplacement);
            }
        }
        double thrustPercent(Vector3D aDirection, Vector3D aNormal) {
            var result = 0.0;
            var offset = angleBetween(aDirection, aNormal);
            var d = 8.0;
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
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            mGTS = new GTS(this, g);
            init();
            initMission();
            setMissionDamp();
            mListener = IGC.RegisterBroadcastListener("docks");
            mListener.SetMessageCallback("docks");
        }

        
        void init() {            
            
            mGTS.get(ref mRC);
            mGTS.get(ref mLCD);
            mGTS.get(ref mCon);
            mGTS.get(ref mGyro);

            mThrust = new List<IMyThrust>();
            mGTS.initList(mThrust);
            initMass();
            ThrustN(0);
            initVelocity();
            
            mGyro.SetValueFloat("Yaw", 0);
            mGyro.SetValueFloat("Pitch", 0);
            mGyro.SetValueFloat("Roll", 0);
            
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
        
        void initSensor() {
            var list = new List<MyDetectedEntityInfo>();
            mSensor.DetectedEntities(list);
            var vec = Vector3D.Zero;
            foreach (var e in list) {
                log.log(e.Name);
            }
        }
        void initAltitude() {
            double altitude;
            if (mRC.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude)) {
                mdAltitude = altitude;
                if (mRC.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude)) {
                    if (altitude < mdAltitude) {
                        mdAltitude = altitude;
                    }
                }
            } else {
                mdAltitude = 0.0;
            }
            log.log("altitude ", mdAltitude);
        }
        /// <summary>
        /// 1 N = 0.10197 kg × 9.80665
        /// mass = N / accel 
        /// stop distance = (velocity^2)/(2* acceleration)
        /// </summary>
        void initMass() {
            var sm = mRC.CalculateShipMass();

            //mdMass = 0;
            mdMass = sm.PhysicalMass;
            
            //absMax(sm.BaseMass, ref mdMass);
            //absMax(sm.PhysicalMass, ref mdMass);
            //absMax(sm.TotalMass, ref mdMass);
            //mdAcceleration = mdNewtons / mdMass;

            //mdAcceleration -= mRC.GetNaturalGravity().Length();

            // stop distance 50 = (10 * 10)/(2 * 1)
            // 0.5 = 1 / 2
        }
        double forceOfVelocity(double mass, double velocity, double time) => mass * velocity / time;
        double momentum(double force, double time) => force * time;
        double forceOfMomentum(double momentum, double time) => momentum / time;
        double acceleration(double force, double mass) => force / mass;
        double forceOfAcceleration(double mass, double acceleration) => mass * acceleration;
        double accelerationFromDelta(double deltaVelocity, double deltaTime) => deltaVelocity / deltaTime;


        Vector3D trajectory2() {
            var sv = mRC.GetShipVelocities();

            // target vector
            var target = new Vector3D(19688.65, 144291, -109020.29);
            var targetVec = mRC.CenterOfMass - target;
            var targetMag = targetVec.Length();
            var targetDir = targetVec / targetMag;
            var targetForce = forceOfVelocity(mdMass, 10.0, mdTimeFactor); // will need to work out
            targetVec = targetDir * targetForce;
            // target force velocity part of this 10.0 needs to vary based on current velo?

            // counteract gravity
            var gravVec = mRC.GetNaturalGravity();
            var gravMag = gravVec.Length();
            var gravDir = gravVec / gravMag;
            var gravForce = forceOfAcceleration(mdMass, gravMag);
            gravVec = gravDir * gravForce;

            // answer velocity
            var veloVec = -sv.LinearVelocity;
            var veloMag = veloVec.Length();
            var veloDir = veloVec / veloMag;
            var veloForce = forceOfVelocity(mdMass, veloMag, mdTimeFactor);
            veloVec = veloDir * veloForce;

            return -gravVec + -veloVec + targetVec;

            
        }
        
        double trajectory(Vector3D aObjective, bool aGyroHold) {
            var sv = mRC.GetShipVelocities();

            //var velocityDir = velocityVec / velocityMag;
            //var velocityForce = forceOfVelocity(mdMass, velocityMag, mdTimeFactor);

            // 100 = 1000 * 0.1
            // mag / dist
            // if result > 1.0 result = 1.0

            // answer target
            //var target = new Vector3D(19688.65, 144291, -109020.29);
            var targetVec = mRC.CenterOfMass - aObjective;
            var targetMag = targetVec.Length();
            var targetDir = targetVec / targetMag;
            var prefVelo = targetMag / 500.0;
            
            if (prefVelo > 1.0) {
                prefVelo = 1.0;
            }
            var maxvelo = 60.0;
            prefVelo *= maxvelo;
            if (targetMag < 1.0) {
                prefVelo = 0.0;
            }

            var targetForce = forceOfVelocity(mdMass, prefVelo, mdTimeFactor); // will need to work out
            targetVec = targetDir * targetForce;

            // counteract gravity
            var gravVec = -mRC.GetNaturalGravity();
            var gravMag = gravVec.Length();
            var gravDir = gravVec / gravMag;
            var gravForce = forceOfAcceleration(mdMass, gravMag);
            gravVec = gravDir * gravForce;

            // counteract velocity
            var veloVec = -sv.LinearVelocity;
            var veloMag = veloVec.Length();

            if (targetMag < 1.0 && veloMag < 0.001) {
                ThrustN(0);
                mGyro.GyroOverride = false;
                return 0.0;
            } else {
                log.log("trajectory targetMag ", targetMag);
                log.log("trajectory veloMag ", veloMag.ToString());
            }
            
            var veloDir = veloVec / veloMag;
            var veloForce = forceOfVelocity(mdMass, veloMag, mdTimeFactor);
            veloVec = veloDir * veloForce;

            // want to use gravity to counteract velocity "below" the plane defined by gravity and target
            // want to use gravity to counteract velocity above the plane defined by -gravity and target
            if (false && gravMag > 2.3) {

                var up = -gravDir;
                var dot = up.Dot(veloDir);

                if (dot > 0) {
                    veloVec = reject(mRC.CenterOfMass + veloVec, mRC.CenterOfMass, up) - mRC.CenterOfMass;
                    veloMag = veloVec.Length();
                    veloDir = veloVec / veloMag;
                    veloForce = forceOfVelocity(mdMass, veloMag, mdTimeFactor);
                    gravForce =
                    gravMag = 0.0;
                }
            }

            /*
             * If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
             * compute the dot product v . (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
             */

            /*
            
            var up = Vector3D.Normalize(mRC.GetNaturalGravity() * -1);
            var dot = up.Dot(Vector3D.Normalize(mvCoM - missionMiddle));
            // norm = dirToShipFromMiddle cross directionToObjectiveFromMiddle
            // dot = dir to obj dot norm
            // var norm = Vector3D.Normalize(aDirection.Cross(matrix.Forward));
            // var dot = matrix.Up.Dot(norm);

            var projection = project(mvCoM, missionMiddle, up);
            var distFromPlane = (mvCoM - projection).Length();
            var missionRadius = missionDistance * 0.5;
            var distance2projection = (projection - missionMiddle).Length();

            log("remainingDistance ", remainingDistance);
            log("distFromPlane ", distFromPlane);
            if (dot > 0) {
                log("above plane");
            } else {
                log("below plane");
            }
            */
            Vector3D extVec;
            //Vector3D extDir;
            
            if (gravMag == 0.0) {
                extVec = veloVec + -targetVec;
                //extDir = Vector3D.Normalize(extVec);
            } else {
                extVec = veloVec + gravVec + -targetVec;
                //extDir = Vector3D.Normalize(veloDir + (gravDir * 10) + -targetDir);
            }
            var extDir = Vector3D.Normalize(extVec);
            var extForce = extVec.Length();

            //var extMag = extVec.Length();
            
            //var requiredForce = mdMass * extMag / mdTimeFactor;
            if (false && gravMag == 0.0) {
                if (veloMag == 0.0) {
                    extForce = mdNewtons;
                } else if (veloMag < 1.0) {
                    extForce *= veloMag;
                } else if (veloMag < 10.0) {
                    extForce *= (veloMag * 0.1);
                }
            }
            //var requiredVec = (velocityDir * velocityForce) + (gravityDir * gravityForce);
            //var requiredMag = requiredVec.Length();
            //var requiredDir = requiredVec / requiredMag;
            //var requiredForce = forceOfVelocity(mdMass, velocityMag, 0.18);
            //var desired = Vector3D.Normalize(new Vector3D(19688.65, 144291, -109020.29) - mRC.CenterOfMass);

            /*
             Try Update10, and apply force of 
            (max_acceleration / (velocity * 3.0)) * max_force
            Where the 3 is 
            0.5 * (60 / update_every_x_ticks) -> 0.5 is a scaling factor of hom smooth the slowing down should be to prevent overshooting
            */



            //Plugin in your current speed into u, and 0 into v
            //Use the formula without t, as that is unknown
            // me try 

            var m = mRC.WorldMatrix;
            
            if (!aGyroHold) {
                // pitch right up down
                // roll forward up down
                //rotate2direction("Pitch", requiredDir, m.Right, m.Up, m.Down);
                rotate2direction("Pitch", extDir, m.Right, m.Up, m.Down);
                //rotate2direction("Roll", requiredDir, m.Forward, m.Up, m.Down);
                rotate2direction("Roll", extDir, m.Forward, m.Up, m.Down);
            } else {
                mGyro.GyroOverride = false;
            }
            //log.log("Required force ", extForce);
            if (true) {
                ThrustN(extForce * thrustPercent(extDir, mRC.WorldMatrix.Up));
            } else {
                ThrustN(0.0f);
            }
            // force = mass x(velocity / time) = (mass x velocity) / time = momentum / time
            // if p = mv and m is constant, then F = dp/dt = m*dv/dt = ma

            // f = ma

            return targetMag;
        }

        void initVelocity() {
            var sv = mRC.GetShipVelocities();

            mvLinearVelocity = sv.LinearVelocity;
            mdLinearVelocity = mvLinearVelocity.Length();

            mvAngularVelocity = sv.AngularVelocity;
            mdAngularVelocity = mvAngularVelocity.Length();

            mvLinearVelocityDirection = mvLinearVelocity / mdLinearVelocity;
            mdStopDistance = (mdLinearVelocity * mdLinearVelocity) / (mdMaxAccel * 2);
            mdStopDistance *= 12.0;

            if (mdStopDistance < 1.0) {
                //mdStopDistance = 1.0;
            }

            // d           = (1                * 1               ) / (2              * 2)
            // d = 0.25
            // veloSquared = d * (accel * 2)
            // velo = sqrt(veloSquared)

            mdDistance2Objective = (mvMissionObjective - mRC.CenterOfMass).Length();
            // pref             = (1000                 / 500) = 2
            // pref             = (500                 / 500) = 1
            // pref             = (250                 / 500) = 0.5

            mdPreferredVelocity = (mdDistance2Objective / mdStopDistance) * mdLinearVelocity;
            mdPreferredVelocity--;

            //var distanceFromStart = (mvMissionStart - mvCoM).Length();
            if (mdDistance2Objective < 0.25) {
                mdPreferredVelocity = 0.0;
            } else if (mdDistance2Objective < 100.0 && mdPreferredVelocity > mdDistance2Objective * 0.1) {
                mdPreferredVelocity = mdDistance2Objective * 0.1;
            }

            if (double.IsNaN(mdPreferredVelocity)) {
                mdPreferredVelocity = MIN_VELO;
            } else if (mdPreferredVelocity < MIN_VELO) {
                mdPreferredVelocity = MIN_VELO;
            } else if (mdPreferredVelocity > MAX_VELO) {
                mdPreferredVelocity = MAX_VELO;
            }

            
            log.log("Distance to Objective ", mdDistance2Objective);
            
            //log("acceleration ", mdAcceleration);
            log.log("linear velocity ", mdLinearVelocity);
            //log("angular velocity ", mdAngularVelocity);
            log.log("stop distance ", mdStopDistance);
            log.log("perferred velocity ", mdPreferredVelocity);
        }

        void receiveMessage() {
            try {
                while (mListener.HasPendingMessage) {
                    var msg = mListener.AcceptMessage();
                    switch (msg.Tag) {
                        case "docks":
                            Connector.FromCollection(msg.Data, mDocks);
                            break;
                    }
                }
            } catch (Exception ex) {
                Me.CustomData = ex.ToString();
            }
        }
        void setMissionTest() {
            initMission();
            meMission = Missions.test;
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
                            case "test":
                                setMissionTest();
                                break;
                            case "rotate":
                                setMissionTrajectory(false);
                                break;
                            case "thrust":
                                setMissionTrajectory(true);
                                break;
                        }
                    }
                }
            }
            if (aUpdate.HasFlag(UpdateType.IGC)) {
                receiveMessage();
            }
            
            if (aUpdate.HasFlag(UpdateType.Update10)) {
                miCount++;
                if (true || miInterval == miCount) {
                    miCount = 0;
                    /*foreach (var d in mDocks.Values) {
                        log(d.Name);
                    }*/
                    try {
                        var v1 = new Vector3D(19715.48, 143953.6, -109091.22);
                        var v2 = new Vector3D(19715.92, 143956.15, -109089.43);
                        log.log("DIST! ", (v1 - v2).Length().ToString());
                        //initSensor();
                        initAltitude();
                        initVelocity();
                        if (null != mMissionConnector) {
                            //log.log(gps("Dock Approach", mMissionConnector.Approach));
                            //log.log(gps("Final Approach", mMissionConnector.ApproachFinal));
                            //log.log(gps("Dock Objective", mMissionConnector.Position));
                            //log.log(gps("Dock Direction", mMissionConnector.Position + mMissionConnector.Direction));
                        }
                        doMission();
                        str = log.clear();
                        if (null != mLCD) {
                            mLCD.WriteText(str);
                        }
                    } catch (Exception ex) {
                        log.log("Handled!", ex);
                        str = log.clear();
                    }
                    
                    Echo(str);
                }
            }
        }
        void ThrustN(double aNewtons) => ThrustN((float)aNewtons);
        void ThrustN(float aNewtons) {
            float fMax, fPercent;
            mdNewtons = 0;
            //log.log("ThrustN requested ", aNewtons, "N");
            for (int i = 0; i < mThrust.Count; i++) {
                
                var t = mThrust[i];
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
                    //log.log("Thruster #", i, " at ", 100.0 * fPercent, "%");
                    t.Enabled = true;
                    t.ThrustOverridePercentage = fPercent;
                } else {
                    //log.log("Thruster #", i, " disabled");
                    t.Enabled = false;
                }
            }
            mdMaxAccel = mdNewtons / mdMass;
            
        }
        void motor2Angle(IMyMotorStator aHinge, float aAngle) {
            if (check(aHinge)) {
                var delta = aAngle - aHinge.Angle;
                log.log("hinge delta", delta);
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
                log.log("check failed");
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
                    // norm = dir to me cross grav
                    // dot = dir to obj dot norm
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
                    log.log("angle nan");
                }
                aRoto.TargetVelocityRad = (float)(v * 6.0);
            }
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
            patrol,
            test,
            thrust,
            rotate,
            none
        }
        enum DampStep : int
        {
            stop,
            hold
        }

        Connector mMissionConnector; 
        
        Dictionary<long, Connector> mDocks = new Dictionary<long, Connector>();
        

        double mdAltitude;
        
        double mdLinearVelocity = 0.0;
        double mdAngularVelocity = 0.0;
        double mdPreferredVelocity = 0.0;
        double mdMass;
        //double mdAcceleration;
        double mdStopDistance;
        double mdDistance2Objective;
        double mdNewtons;
        double mdMaxAccel;
        double mdMissionAltitude = 0;
        const double mdRotateEpsilon = 0.1;
        double mdMissionDistance = 0.0;

        readonly GTS mGTS;
        readonly Logger g;

        List<IMyThrust> mThrust;
        IMyTextPanel mLCD;
        readonly IMyBroadcastListener mListener;
        IMyRemoteControl mRC;
        IMyGyro mGyro;
        IMySensorBlock mSensor;
        IMyShipConnector mCon;


        int miDock = 0;
        int miCount = 0;
        const int miInterval = 10;
        const double mdTickTime = 1.0 / 60.0;
        const double mdTimeFactor = mdTickTime * miInterval;
        int miMissionStep = 0;

        Logger log = new Logger();

        Missions meMission = Missions.damp;

        
        Vector3D PATROL_0 = new Vector3D(45519.94, 164664.93, -85803.92);
        Vector3D PATROL_1 = new Vector3D(46015.06, 164066.36, -86526.14);
        Vector3D MOON_DOCK = new Vector3D(19706.77, 143964.78, -109088.83);

        
        Vector3D mvMissionObjective;
        Vector3D mvMissionStart;
        Vector3D mvTranslation;
        //Vector3D mvCoM;

        
        Vector3D mvLinearVelocity = Vector3D.Zero;
        Vector3D mvAngularVelocity = Vector3D.Zero;
        Vector3D mvLinearVelocityDirection = Vector3D.Zero;

    }
    // large connectors distance apart 2.65 
    // small connector distance from large 1.85
    // small connector distance from small 1.00

    /*
        Vector3D otherside = new Vector3D(986148.14, 102603.57, 1599688.09);
        Vector3D BASE_HIGHER = new Vector3D(1045917.97, 142402.91, 1571139.78);
        Vector3D BASE_SPACE_1 = new Vector3D(44710.14, 164718.97, -85304.59);
        Vector3D BASE_SPACE_2 = new Vector3D(44282.68, 164548.94, -85064.41);
        Vector3D BASE_SPACE_3 = new Vector3D(44496.03, 164633.07, -85185.32);
      
    */
}
