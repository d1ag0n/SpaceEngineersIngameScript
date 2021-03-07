using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //·²‖
        MyDetectedEntityInfo? mPlanet;
        
        /*public void rotate2vector(Vector3D aTarget) {
            double result = 0;
            if (Vector3D.Zero == aTarget) {
                g.log("rotate2vector zero");
                setGyrosOverride(false);
            } else {
                g.log("rotate2vector", aTarget);
                setGyrosOverride(true);
                ApplyGyroOverride(pitch2vector(aTarget), 0.0, roll2vector(aTarget));
                //result += Math.Abs(pitch2vector(aTarget));
                //result += Math.Abs(roll2vector(aTarget));
            }
            //return result;
        }*/
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
        void setBatteryCharge(ChargeMode aMode) {
            foreach (var b in mBatteries) {
                b.ChargeMode = aMode;
            }
        }
        void setRefuel(bool aRefuel) {
            foreach (var t in mFuelTanks) {
                t.Stockpile = aRefuel;
            }
        }
        bool fullFuel() {
            foreach (var t in mFuelTanks) {
                if (t.FilledRatio < 0.999) {
                    return false;
                }
            }
            return true;
        }
        bool lowFuel() {
            foreach (var t in mFuelTanks) {
                if (0.1 > t.FilledRatio) {
                    return true;
                }
            }
            return false;
        }
        bool fullCharge() {
            foreach (var b in mBatteries) {
                if (b.CurrentStoredPower / b.MaxStoredPower < 0.95) {
                    if (b is IMyPowerProducer) {
                        var pp = b as IMyPowerProducer;
                    }
                    return false;
                }
            }
            return true;
        }
        bool lowBattery() {
            foreach (var b in mBatteries) {
                if (0.20 > b.CurrentStoredPower / b.MaxStoredPower) {
                    return true;
                }
            }
            return false;
        }
        double rotate2target(string aGyroOverride, Vector3D aTarget, Vector3D aPlane, Vector3D aNormal, Vector3D aIntersect1, Vector3D aIntersect2) {
            // yaw
            // m.Translation = aPlane
            // m.Up = aNormal
            // m.Forward = aIntersect
            var position = MAF.orthoProject(aTarget, aPlane, aNormal);
            var displacement = position - aPlane;
            var direction = Vector3D.Normalize(displacement);
            return rotate2direction(aGyroOverride, direction, aNormal, aIntersect1, aIntersect2);
        }
        double rotate2direction(string aGyroOverride, Vector3D aDirection, Vector3D aNormal, Vector3D aIntersect1, Vector3D aIntersect2) {
            //log("rotate2direction");
            var angle = MAF.angleBetween(aDirection, aIntersect1);
            //log(aGyroOverride, " angle ", angle);
            
            if (angle > mdRotateEpsilon) {
                var norm = Vector3D.Normalize(aDirection.Cross(aIntersect2));
                var dot = aNormal.Dot(norm);
                if (dot < 0) {
                    angle = -angle;
                }
            } else {
                angle = 0;
            }
            //g.log("rotate2direction", aGyroOverride, " ", rpm);
            //mGyro.GyroOverride = true;
            //mGyro.SetValueFloat(aGyroOverride.ToString(), (float)rpm);
            //log(aGyroOverride, " rpm ", rpm);
            return angle;
        }
        
        
        
        
        void absMax(double a, ref double b) {
            a = Math.Abs(a);
            if (a > b) {
                b = a;
            }
        }
        double rot2rpm(double x, double scale) {
            if (x > 1.0) {
                g.log("BAD ", x);
            } else if (x < -1.0) {
                g.log("BAD ", x);
            }
            var result = Math.Asin(x) / 2.0 / Math.PI * 60.0 / 0.166666;
            if (Math.Abs(result) < 0.001) {
                //result = 0.0;
            }
            if (result > 60.0) {
                g.persist($"dying {result}");
                //Me.Enabled = false;
            }
            return result * scale;
        }
        void setMissionCalibrate() {
            initMission();
            mMission.Detail = Mission.Details.calibrate;
            mMission.Objective = mRC.CenterOfMass;
        }
        void doMissionCalibrate() {
            g.log("mMission.Step ", mMission.Step);
            switch (mMission.Step) {
                case 0:
                    if (false && mdAvailableMass > 0) {
                        g.log($"Please add {mdAvailableMass}kg to cargo.");
                    } else {
                        mMission.Step++;
                    }
                    calculateTrajectory();
                    break;
                case 1:
                    // ensure we're slow and pointing mostly up
                    calculateTrajectory();
                    if (mdDistance2Objective < 1.0) {
                        mMission.Step++;
                        mMission.PendingDirection = Vector3D.Down;
                        mGyro.Rotate(Vector3D.Zero);
                    }
                    break;
                case 2:
                    if (mdGravity > 0) {
                        ThrustN(mdNewtons);
                    } else {
                        ThrustN(0);
                    }
                    if (calibrate()) {
                        mMission.Step++;
                        mMission.PendingDirection = Vector3D.Up;
                    }
                    break;
                case 3:
                    if (mdGravity > 0) {
                        ThrustN(mdNewtons);
                    } else {
                        ThrustN(0);
                    }
                    if (calibrate()) {
                        mdRotationTime = time;
                        mMission.Step++;
                        mMission.PendingDirection = Vector3D.Down;
                    }
                    break;
                case 4:
                    if (mdGravity > 0) {
                        ThrustN(mdNewtons);
                    } else {
                        ThrustN(0);
                    }
                    if (calibrate()) {
                        mdRotationTime = time - mdRotationTime;
                        var obj = mMission.Objective;
                        setMissionDamp();
                        mMission.Objective = obj;
                    }
                    break;
            }
        }
        double mdRotationTime = 5.0;
        const double timeFlashMax = .5; //in seconds  
        const double updatesPerSecond = 10;
        const double proportionalConstant = 2.0;
        const double integralConstant = 0.0;
        const double derivativeConstant = 0.9;
        const double pidLimit = 10;
        const double timeLimit = 1 / updatesPerSecond;
        readonly PID pidPitch = new PID(proportionalConstant, integralConstant, derivativeConstant, -pidLimit, pidLimit, timeLimit);
        readonly PID pidRoll = new PID(proportionalConstant, integralConstant, derivativeConstant, -pidLimit, pidLimit, timeLimit);
        bool calibrate() {
            
            var mat = mRC.WorldMatrix;
            mGyro.Rotate(mMission.PendingDirection);
            /*ApplyGyroOverride(
                rotate2direction("Pitch", mMission.PendingDirection, mat.Right, mat.Up, mat.Down),
                0,
                rotate2direction("Roll", mMission.PendingDirection, mat.Forward, mat.Up, mat.Down)
            );*/
            var ab = MAF.angleBetween(mRC.WorldMatrix.Down, mMission.PendingDirection);
            g.log("calibrate");
            g.log("angle              ", ab);
            g.log("angularVeloSquared ", mdAngularVelocitySquared);
            var result = ab < Math.PI / 180.0 && mdAngularVelocitySquared < Math.PI / 180.0;
            g.log("result ", result);
            return result;
        }
        void setMissionDamp() {
            initMission();
            mMission.Objective = mRC.CenterOfMass - mvGravityDirection;
        }
        void setMissionFollow() {
            initMission();
            mMission.Objective = mRC.CenterOfMass;
            mMission.Detail = Mission.Details.follow;
        }
        
        void doMissionDamp() {
            switch (mCon.Status) {
                case MyShipConnectorStatus.Connectable:
                    mGyro.Rotate(Vector3D.Zero);
                    ThrustN(mdMass * mdGravity);
                    break;
                case MyShipConnectorStatus.Connected:
                    ThrustN(0);
                    mGyro.Rotate(Vector3D.Zero);
                    break;
                case MyShipConnectorStatus.Unconnected:
                    calculateTrajectory();
                    break;
            }
            
        }

        void dock() {
            setBatteryCharge(ChargeMode.Recharge);
            setRefuel(true);
            mGyro.setGyrosEnabled(false);
            ThrustN(0);
        }
        void undock() {
            setBatteryCharge(ChargeMode.Discharge);
            setRefuel(false);
            mGyro.setGyrosEnabled(true);
            mCon.Enabled = false;
        }
        void setBoxNavigation(string aWaypointName) {
            MyWaypointInfo waypoint = MyWaypointInfo.Empty;
            if (findWaypoint(aWaypointName, ref waypoint)) {
                initMission();
                undock();                
                mMission.Detail = Mission.Details.boxnav;
                mMission.Objective = mRC.CenterOfMass - mvGravityDirection;
                mMission.Translation = waypoint.Coords;
            }
        }
        void setMissionNavigate(string aWaypointName) {
            MyWaypointInfo waypoint = MyWaypointInfo.Empty;
            if (findWaypoint(aWaypointName, ref waypoint)) {
                initMission();
                undock();
                mMission.Detail = Mission.Details.navigate;
                mMission.Objective = waypoint.Coords;

            }
        }
        bool findWaypoint(string aName, ref MyWaypointInfo aWaypoint) {
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
            mMission.Detail = Mission.Details.patrol;
        }
        /*
        //Whip's ApplyGyroOverride Method v10 - 8/19/17
        void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed) {

            //pitch_speed = pidPitch.Control(pitch_speed);
            //roll_speed = pidRoll.Control(roll_speed);
            // Large gyro 3.36E+07
            // Small gyro 448000
            var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs             
            
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, mRC.WorldMatrix);
            

            //g.log("ApplyGyroOverride");
            foreach (var gy in mGyros) {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gy.WorldMatrix));
                //g.log("transformedRotationVec", transformedRotationVec);
                gy.GyroOverride = true;

                gy.Pitch = (float)transformedRotationVec.X;
                gy.Yaw = (float)transformedRotationVec.Y;
                gy.Roll = (float)transformedRotationVec.Z;
            }
        }*/

        int scanNavCount = 0;
        
        
        readonly HashSet<Vector3L> mVisits = new HashSet<Vector3L>();

        double mdScanBoxDist;

        void setMissionScan(string aWaypointName) {
            if (scanner.hasCamera) {
                var wp = MyWaypointInfo.Empty;
                if (findWaypoint(aWaypointName, ref wp)) {
                    initMission();

                    mVisits.Clear();

                    scanNavCount = 0;
          
                    mMission.Detail = Mission.Details.scan;
                    
                    // mvMissionTranslation is used as the original location to scan in
                    mMission.Translation = wp.Coords;

                    // will modify mMission.Objective to navigate in the scan zone as necessary
                    mMission.Objective = Me.CubeGrid.WorldAABB.Center;

                    mbScan =
                    mbScanGood = Me.CubeGrid.WorldAABB;
                    mdScanBoxDist = (mbScan.Max - mbScan.Min).Length();
                    mbScanComplete = true;
                }
            } else {
                g.persist("Camera required for scan mission.");
            }
        }
        

        int miScanStep;
        bool mbScanComplete;
        BoundingBoxD mbScan;
        int setScanCount = 0;
        void setScan(BoundingBoxD aBox, double aInflate = mdScanInflate) {
            setScanCount++;
            miScanStep = -1;
            mbScanComplete = false;
            mbScan = aBox.Inflate(aInflate);

            //g.persist(g.gps("ss#" + setScanCount, mbScan.Center));
            mDetected.Clear();
            aBox.GetCorners(mvaCorners);
            int i = 0;
            /*foreach (var c in mvaCorners) {
                g.persist(g.gps("ss#" + setScanCount + "." + i, c));
                i++;
            }*/

        }
        //bool stepOkay = false;
        void doScan(double aInflate = mdScanInflate) {
            g.log("doScan");
            g.log("mbScanComplete ", mbScanComplete);
            g.log("miScanStep ", miScanStep);
            g.log(mbScan);
            if (!mbScanComplete) {
                if (miScanStep > -1 && miScanStep < 8) {
                    if (_doScan(mvaCorners[miScanStep])) {
                        miScanStep++;
                        if (miScanStep == 8) {
                            //g.persist(g.gps("scanned", mbScan.Center));
                            mbScanComplete = true;
                            if (mDetected.Count == 0) {
                                mbScanGood = mbScan.Inflate(-aInflate);
                            }
                        }
                    }
                }
                if (miScanStep == -1) {
                    if (_doScan(mbScan.Center, mdScanBoxDist)) {
                        miScanStep++;
                    }
                }
            }
        }
        // returns true if target was scanned
        bool _doScan(Vector3D aTarget, double aAddDist = 0) {
            g.log("_doScan", aTarget);
            var e = new MyDetectedEntityInfo();
            var disp = aTarget - mRC.CenterOfMass;
            var len2 = disp.LengthSquared();
            if (len2 > 25000000) {
                g.log("too far");
                return false;
            }
            if (Me.CubeGrid.WorldAABB.Contains(aTarget) == ContainmentType.Contains) {
                return true;
            }
            if (scanner.Scan(aTarget, ref e, aAddDist)) {

                if (e.Type == MyDetectedEntityType.None) {
                    g.log("could scan");
                    return true;
                } else {
                    mDetected.Add(e);
                    if (!mDetectedIds.Contains(e.EntityId)) {
                        mDetectedIds.Add(e.EntityId);
                    }
                    g.log("could scan");
                    return true;
                }
            }
            bYawAround = true;
            g.log(g.gps("CouldNotScan", aTarget));
            return false;
        }
        
        const double mdScanInflate = 20;
        readonly Random random = new Random(9);
        Vector3D ranDir() => Vector3D.Normalize(new Vector3D(random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5));
        Vector3D ranBoxPos(BoundingBoxD aBox) => 
            new Vector3D(
                aBox.Min.X + random.NextDouble() * (aBox.Max.X - aBox.Min.X), 
                aBox.Min.Y + random.NextDouble() * (aBox.Max.Y - aBox.Min.Y), 
                aBox.Min.Z + random.NextDouble() * (aBox.Max.Z - aBox.Min.Z)
            );
        
        BoundingBoxD mbScanGood;
        //bool stepOkay = false;
        void doMissionScan() {
            g.log("doMissionScan");
            g.log(g.gps("scan good", mbScanGood.Center));
            // mvMissionTranslation is used as the original location to scan in
            // will modify mMission.Objective to navigate in the scan zone as necessary

            // mMission.Step
            // 0 = scan to direction of mvMissionTranslation
            // 1 = 0 found collision, if in gravity check above, in space check around
            // 2 = 1 found collision, work through 26 directions, miMissionSubStep
            // 3 = 2 found collision, work through random directions
            // 4 = ??
            //var detected = false;

            if (mDetected.Count > 0 && mMission.Step < 2) {
                if (mbScanGood.Contains(mRC.CenterOfMass) == ContainmentType.Contains && mbScan.Contains(mMission.Translation) == ContainmentType.Contains) {
                    g.persist("damping at last good scan");
                    var t = mbScanGood.Center;
                    setMissionDamp();
                    mMission.Objective = t;
                    return;
                }
                mMission.Step++;
                if (2 == mMission.Step) {
                    mMission.SubStep = 0;
                } else if (4 == mMission.Step) {
                    mMission.Step = 0;
                }
                mbScanComplete = true;
            } else if (mbScanComplete && mDetected.Count == 0) {
                if (mbScan.Contains(mMission.Translation) == ContainmentType.Contains) {
                    g.persist("damping at objective");
                    var t = mMission.Translation;
                    setMissionDamp();
                    mMission.Objective = t;
                    return;
                }
                mMission.Objective = mbScanGood.Center;
                //if ((mRC.CenterOfMass - mMission.Translation).LengthSquared() > (mbScanGood.Center - mMission.Translation).LengthSquared()) { }
                scanNavCount++;
                //g.persist(g.gps($"{mMission.Step} - {scanNavCount}", mMission.Objective));
                mMission.Step = 0;
            }
            
            //var dir = Vector3D.Normalize(mMission.Translation - mRC.CenterOfMass);
            if (mbScanComplete) {
                if (0 == mMission.Step) {
                    bYawAround = false;
                    setScan(BOX.moveTowardsPos(mbScanGood, mMission.Translation));
                } else if (1 == mMission.Step) {
                    bYawAround = false;                    
                    setScan(BOX.moveTowardsDir(mbScanGood, -mvGravityDirection));
                } else if (2 == mMission.Step) {
                    bYawAround = true;
                    var dir = Vector3D.Zero;
                    switch (mMission.SubStep) {
                        case  0: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Up); break;
                        case  1: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Up + Vector3D.Right); break;
                        case  2: dir = Vector3D.Normalize(Vector3D.Up + Vector3D.Right); break;
                        case  3: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Up + Vector3D.Right); break;
                        case  4: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Up); break;
                        case  5: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Up + Vector3D.Left); break;
                        case  6: dir = Vector3D.Normalize(Vector3D.Up + Vector3D.Left); break;
                        case  7: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Up + Vector3D.Left); break;
                        case  8: dir = Vector3D.Up; break;

                        case  9: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Down); break;
                        case 10: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Down + Vector3D.Right); break;
                        case 11: dir = Vector3D.Normalize(Vector3D.Down + Vector3D.Right); break;
                        case 12: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Down + Vector3D.Right); break;
                        case 13: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Down); break;
                        case 14: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Down + Vector3D.Left); break;
                        case 15: dir = Vector3D.Normalize(Vector3D.Down + Vector3D.Left); break;
                        case 16: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Down + Vector3D.Left); break;
                        case 17: dir = Vector3D.Down; break;

                        case 18: dir = Vector3D.Forward; break;
                        case 19: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Right); break;
                        case 20: dir = Vector3D.Right; break;
                        case 21: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Right); break;
                        case 22: dir = Vector3D.Backward; break;
                        case 23: dir = Vector3D.Normalize(Vector3D.Backward + Vector3D.Left); break;
                        case 24: dir = Vector3D.Left; break;
                        case 25: dir = Vector3D.Normalize(Vector3D.Forward + Vector3D.Left); break;
                    }

                    if (mMission.SubStep < 26) {
                        mMission.SubStep++;
                        setScan(BOX.moveTowardsDir(mbScanGood, dir));
                    } else if (mMission.SubStep == 26) {
                        mMission.Step++;
                    }
                } else {
                    bYawAround = true;
                    setScan(BOX.moveTowardsDir(mbScanGood, ranDir()));
                }
            }
            g.log("mMission.Step    ", mMission.Step);
            g.log("mMission.SubStep ", mMission.SubStep);
            doScan();
            calculateTrajectory();
            
        }
        bool bYawAround = false;
        
        void initMission() {
            bYawAround = false;
            if (null == mMission) {
                mMission = new Mission();
            }
            igcMessagesFailed = igcMessagesSent = 0;
            mMission.Start = mRC.CenterOfMass;
            mMission.Distance =
            mMission.Altitude =
            mMission.SubStep =
            mMission.Step = 0;

            mMission.Detail = Mission.Details.damp;

            mMission.Translation =
            mMission.Objective = Vector3D.Zero;

            mGyro.setGyrosEnabled(true);
            mGyro.Rotate(Vector3D.Zero);
            mMission.Connector = null;

            if (mCon.Status == MyShipConnectorStatus.Unconnected) mCon.Enabled = false;
            miLastTrajectoryPlane = -1;
        }


        void doMission() {
            g.log("doMission ", mMission.Detail);
            switch (mMission.Detail) {
                case Mission.Details.damp:
                    calculateTrajectory();
                    break;
                case Mission.Details.navigate:
                    missionNavigate();
                    break;
                case Mission.Details.dock:
                    doMissionDock();
                    break;
                case Mission.Details.test:
                    //mGyro.SetValueFloat("Pitch", 10.0f);
                    var c = findConnector("con1");
                    var local = -MAF.world2pos(mRC.CenterOfMass, mCon.WorldMatrix);
                    //var objective = local2pos(local, c.World) + (c.World.Forward * 2.65);
                    //log.log(gps("test con1", objective));
                    break;
                case Mission.Details.scan:
                    doMissionScan();
                    break;
                case Mission.Details.calibrate:
                    doMissionCalibrate();
                    break;
                case Mission.Details.follow:
                    doMissionFollow();
                    break;
                case Mission.Details.boxnav:
                    doMissionBoxNav();
                    break;
                default:
                    g.log("mission unhandled");
                    break;
            }
        }
        void doMissionFollow() {
            mDetected.Clear();
            mSensor.DetectedEntities(mDetected);
            foreach (var e in mDetected) {
                g.log(e.Type);
                if (e.Type == MyDetectedEntityType.CharacterHuman) {
                    var p = e.Position - mRC.CenterOfMass;
                    var d = p;
                    var m = d.Normalize();
                    g.log(m);
                    if (m > 30.0) {
                        mMission.Objective = mRC.CenterOfMass + d * 20.0;
                    }
                }
            }
            calculateTrajectory();
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
        bool setMissionDock(string aConnector) {
            var foundConnector = findConnector(aConnector);
            var result = false;
            if (null != foundConnector) {
                initMission();
                mMission.Connector = foundConnector;
                mMission.Connector.MessageSent = 0;
                var approachDistance = 600;
                var finalDistance = approachDistance * 0.5;
                mMission.Detail = Mission.Details.dock;
                var approachPlane = mMission.Connector.Position + (mMission.Connector.Direction * approachDistance);
                mMission.Connector.ApproachFinal = mMission.Connector.Position + (mMission.Connector.Direction * finalDistance);
                var projectedPosition = MAF.orthoProject(mRC.CenterOfMass, approachPlane, mMission.Connector.Direction);
                var projectedDirection = Vector3D.Normalize(projectedPosition - approachPlane);
                mMission.Connector.Approach = approachPlane + (projectedDirection * approachDistance);
                mMission.Connector.Objective = mMission.Connector.Position + (mMission.Connector.Direction * (4.0 + (mCon.WorldMatrix.Translation - mRC.CenterOfMass).Length()));
                result = true;
                var msg = new DockMessage(mMission.Connector.DockId, "Retract", Vector3D.Zero);
                if (IGC.SendUnicastMessage(mMission.Connector.ManagerId, "DockMessage", msg.Data())) {
                    igcMessagesSent++;
                } else {
                    igcMessagesFailed++;
                }
            }
            return result;
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
            charging,
            complete
        }

        void doMissionDock() {
            g.log("Dock Mission: ", mMission.Connector.Name);
            var d = 0.0;
            var msg = "unknown";
            var step = (DockStep)mMission.Step;
            switch (step) {
                case DockStep.departFinal:
                case DockStep.approach:
                    mMission.Objective = mMission.Connector.Approach;
                    if (DockStep.departFinal == step) {
                        msg = "depart approach area";
                    } else {
                        msg = "rendezvous with approach";
                    }
                    // goto initial approach
                    d = distance2objective();
                    if (250.0 > d) {
                        if (DockStep.departFinal == step) {
                            msg = "depart approach area";
                            mMission.Step = (int)DockStep.complete;
                        } else {
                            msg = "rendezvous with approach";
                            mMission.Step++;
                        }
                        
                    }
                    missionNavigate();
                    break;
                case DockStep.approachFinal:
                case DockStep.depart:
                    mMission.Objective = mMission.Connector.ApproachFinal;
                    msg = "rendezvous with final approach";
                    var precision = 10.0;
                    if (DockStep.depart == step) {
                        msg = "depart dock area";
                        precision = 100.0;
                    }
                    // goto beginning of final approach
                    
                    if (precision > missionNavigate()) {
                        if (step == DockStep.approachFinal) {
                            mMission.Objective = mMission.Connector.Objective;
                        }
                        mMission.Step++;
                    }
                    break;
                case DockStep.dock:
                    msg = "rendezvous with dock";
                    d = missionNavigate(false);
                    
                    if (d < 5.0) {
                        mCon.Enabled = true;
                        mMission.Step = (int)DockStep.connect;
                        if (0 == mdGravity) {
                            mGyro.Rotate(Vector3D.Zero);
                            ThrustN(0);
                        }
                    }
                    break;
                case DockStep.connect:
                    msg = "connecting to dock";
                    switch (mCon.Status) {
                        case MyShipConnectorStatus.Connectable:
                            if (3 == mMission.Connector.MessageSent) {
                                var dockMessage = new DockMessage(mMission.Connector.DockId, "Align", mCon.WorldMatrix.Translation);
                                if (IGC.SendUnicastMessage(mMission.Connector.ManagerId, "DockMessage", dockMessage.Data())) {
                                    igcMessagesSent++;
                                    mMission.Connector.MessageSent = 0;
                                } else {
                                    igcMessagesFailed++;
                                }
                            } else {
                                mMission.Connector.MessageSent++;
                            }
                            ThrustN(mdMass * mdGravity);
                            mGyro.Rotate(Vector3D.Zero);
                            break;
                        case MyShipConnectorStatus.Unconnected:
                            if (3 == mMission.Connector.MessageSent) {
                                var dockMessage = new DockMessage(mMission.Connector.DockId, "Align", mCon.WorldMatrix.Translation);
                                if (IGC.SendUnicastMessage(mMission.Connector.ManagerId, "DockMessage", dockMessage.Data())) {
                                    igcMessagesSent++;
                                    mMission.Connector.MessageSent = 0;
                                } else {
                                    igcMessagesFailed++;
                                }
                            } else {
                                mMission.Connector.MessageSent++;
                            }
                            if (mdGravity == 0) {
                                mGyro.Rotate(mRC.CenterOfMass + (mMission.Connector.Direction * 500.0));
                            } else {
                                calculateTrajectory(true);
                            }
                            
                            break;
                        case MyShipConnectorStatus.Connected:
                            dock();
                            mMission.Step = (int)DockStep.wait;
                            break;
                    }
                    break;
                case DockStep.wait:
                    msg = "connected to dock";
                    if (mCon.Status == MyShipConnectorStatus.Unconnected) {
                        mMission.Step++;
                    }
                    /*if (lowBattery() || lowFuel()) {
                        mMission.Step = (int)DockStep.charging;
                    } else {
                        mMission.Step = (int)DockStep.depart;
                        mbAutoCharge = false;
                        undock();
                    }*/
                    break;
                case DockStep.charging:
                    msg = "refuel and recharge";
                    if (fullCharge() && fullFuel()) {
                        mMission.Step = (int)DockStep.depart;
                        mbAutoCharge = false;
                        undock();
                    }
                    break;
                case DockStep.complete:
                    msg = "departure complete";
                    setMissionDamp();
                    break;
                default:
                    g.log("step unhandled, damping");
                    doMissionDamp();
                    break;
            }
            g.log(msg);
        }
        
        void doMissionBoxNav() {
            calculateTrajectory();
            // mMission.Translation will be final objective
            // mMission.Objective will be center of cboxes
            // steps
            // ~ any obstruction in current cbox should pause and move to step n
            // 0 get next cbox in direction of objective
            // 1 scan cbox / wait for reservation
            //   - if obstructed report scan obstruction 1 time
            //     - move to step n
            //   - once reservation is received move to step 2
            // 2 navigate to cbox center
            //   - continue scanning random positions in target cbox
            //   - if obstruction scanned damp at current position, report obstruction 1 time, move to step n
            //   - once inside the cbox continue to step 0
            BoundingBoxD cbox, dest;
            g.log("mMission.Step    ", mMission.Step);
            g.log("mMission.SubStep ", mMission.SubStep);
            g.log("miScanStep       ", miScanStep);
            
            switch (mMission.Step) {
                case 0:
                    cbox = BOX.GetCBox(mRC.CenterOfMass);
                    if (cbox.Contains(mMission.Translation) == ContainmentType.Contains) {
                        mMission.Step = 4;
                    } else {
                        dest = BOX.GetCBox(BOX.MoveC(cbox.Center, Vector3D.Normalize(mMission.Translation - mRC.CenterOfMass)));                        
                        mMission.PendingPosition = dest.Center;
                        setScan(dest, 0);
                        mMission.Step++;
                    }
                    break;
                case 1:
                    doBoxScan();
                    if (mDetected.Count > 0) {
                        g.persist("1 pausing");
                        boxNavPause();
                    } else {
                        if (getReservation(mMission.PendingPosition)) {
                            mMission.Step++;
                            mMission.Objective = mMission.PendingPosition;
                            g.persist(g.gps("to", mMission.Objective));
                        }
                    }
                    break;
                case 2:
                    doBoxScan();
                    if (mDetected.Count > 0) {
                        g.persist("2 pausing");
                        boxNavPause();
                    } else {
                        cbox = BOX.GetCBox(mMission.PendingPosition);
                        var ct = cbox.Contains(mRC.CenterOfMass);
                        g.log(ct);
                        if (ct == ContainmentType.Contains) {
                            g.persist("2 stepping to 0");
                            mMission.Step = 0;
                            mMission.SubStep = 0;
                        } else {
                            g.log(g.gps("pendPos", cbox.Center));
                            foreach (var c in cbox.GetCorners()) {
                                g.log(g.gps("ppc", c));
                            }
                        }
                    }
                    break;
                case 3: // n
                    // the box in the direction of our objective is obstructed
                    // SubSteps - will need to be reset to 0 at some point probably once we progress 
                    // 0 first attempt at finding another way in gravity we should try -gravity first
                    //   - in space we could just skip to substep n
                    //   - get new cbox, setScan, move to step 1 basically almost everything in step 0 todo extract logic?
                    // default substep - 1 base6direction ?? 7 Stuck?
                    //
                    switch (mMission.SubStep) {
                        case 0:
                            cbox = BOX.GetCBox(mRC.CenterOfMass);
                            dest = BOX.GetCBox(BOX.MoveC(cbox.Center, -mvGravityDirection));
                            mMission.PendingPosition = dest.Center;
                            setScan(dest, 0);
                            g.persist("n0 stepping to 1");
                            mMission.Step = 1;
                            mMission.SubStep++;
                            break;
                        default:
                            if (mMission.SubStep < 7) {
                                cbox = BOX.GetCBox(mRC.CenterOfMass);
                                dest = BOX.GetCBox(BOX.MoveC(cbox.Center, Base6Directions.Directions[mMission.SubStep - 1]));
                                mMission.PendingPosition = dest.Center;
                                setScan(dest, 0);
                                g.persist("nd stepping to 1");
                                mMission.Step = 1;
                                mMission.SubStep++;
                            } else {
                                // todo AABB nav + reservation?
                                // switch back to cbox nav after moving into two cboxes?
                            }
                            break;
                    }
                    break;
                case 4:
                    // we made it
                    // continue navigation to cbox center
                    // continue scanning
                    // ?? switch to AABB navigation
                    doBoxScan();
                    if (mDetected.Count > 0) {
                        g.persist("I almost made it but something got in the way...");
                        setMissionDamp();
                    } else {
                        if (mdDistance2Objective < 5.0) {
                            g.persist("I made it.");
                            setMissionDamp();
                            mMission.Objective = BOX.GetCBox(mRC.CenterOfMass).Center;
                        }
                    }
                    break;
            }
        }
        void boxNavPause() {
            // todo report
            mbScanComplete = true;
            mMission.Objective = mRC.CenterOfMass - mvGravityDirection;
            mMission.Step = 3;
            // todo move to step n
        }
        void doBoxScan() {
            if (!mbScanComplete) {
                switch (miScanStep) {
                    case -1:
                        // scan center
                        if (_doBoxScan(mbScan.Center)) {
                            miScanStep++;
                        }
                        break;
                    case 8:
                        // scan random position
                        _doBoxScan(ranBoxPos(mbScan));
                        break;
                    default:
                        // scan corners
                        if (_doBoxScan(mvaCorners[miScanStep])) {
                            miScanStep++;
                        }
                        break;
                }
            }
        }
        /// <summary>
        /// returns true if aTarget was scanned
        /// </summary>
        /// <param name="aPosition"></param>
        /// <returns></returns>
        bool _doBoxScan(Vector3D aTarget) {
            // todo scan and store in mDetected
            MyDetectedEntityInfo e = new MyDetectedEntityInfo();
            
            if (scanner.Scan(aTarget, ref e)) {
                if (e.Type != MyDetectedEntityType.None) {
                    mDetected.Add(e);
                }
                return true;
            }
            return false;
        }
        bool getReservation(Vector3D aCBoxCenter) {
            // todo getreservation
            return true;
        }
        double missionNavigate(bool aSlow = false) {
            

            calculateTrajectory(aSlow);
            var result = mdDistance2Objective;
            
            if (result < 0.5) {
                result = 0.0;
                
            }
            //g.log("navigate result ", result);
            return result;

            //rotate2vector(Vector3D.Zero);

        }
        double distance2objective() => _distance2(mMission.Objective, mRC.CenterOfMass);
        double _distance2(Vector3D aTarget, Vector3D aOrigin) => (aTarget - aOrigin).Length();
        
        double thrustPercent(Vector3D aDirection, Vector3D aNormal) {
            var result = 0.0;
            var offset = MAF.angleBetween(aDirection, aNormal);
            var d = 4.0;
            if (offset < Math.PI / d) {
                result = 1.0 - (offset / (Math.PI / d));
            }

            return result;
        }

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
            g = new Logger();
            mGTS = new GTS(this, g);
            mGyro = new Gyro(mGTS, g);

            init();


            if (!autoDock()) {
                setMissionDamp();
            }
            mListener = IGC.RegisterBroadcastListener("docks");
            mListener.SetMessageCallback("docks");
        }
        bool autoDock() {
            if (!mbAutoCharge && (lowBattery() || lowFuel())) {
                if (setMissionDock("moon")) {
                    mbAutoCharge = true;
                    return true;
                }
            }
            return false;
        }
        
        void init() {            
            
            mGTS.get(ref mRC);
            mGTS.get(ref mLCD);
            mGTS.get(ref mCon);
            mGTS.get(ref mSensor);
            
            //mGTS.initList(mGyros);
            mGTS.initList(mThrusters);
            mdNewtons = 0;
            foreach (var t in mThrusters) {
                mdNewtons += t.MaxEffectiveThrust;
            }
            mGTS.initList(mBatteries);
            mGTS.initList(mFuelTanks);

            
            scanner = new Scanner(this, mGTS, g);

            

            for (int i = mFuelTanks.Count - 1; i > -1; i--) {
                /*MyObjectBuilder_OxygenTank / LargeHydrogenTank
                MyObjectBuilder_OxygenTank / LargeHydrogenTankSmall
                MyObjectBuilder_OxygenTank / OxygenTankSmall
                MyObjectBuilder_OxygenTank / SmallHydrogenTank
                MyObjectBuilder_OxygenTank / SmallHydrogenTankSmall*/
                var t = mFuelTanks[i];
                var n = t.BlockDefinition.SubtypeName;
                var filter = new string[] { "SmallHydrogenTank", "LargeHydrogenTank" };
                if (!filter.Contains(n)) {
                    mFuelTanks.RemoveAt(i);
                    g.persist($"Removed tank {n}.");
                }
            }
            initMission();
            ThrustN(0);
            initVelocity();
            
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


        /// <summary>
        /// 1 N = 0.10197 kg × 9.80665
        /// mass = N / accel 
        /// stop distance = (velocity^2)/(2* acceleration)
        /// </summary>

        
        double mdAvailableMass;
        double forceOfVelocity(double mass, double velocity, double time) => mass * velocity / time;
        double momentum(double force, double time) => force * time;
        double forceOfMomentum(double momentum, double time) => momentum / time;
        double acceleration(double force, double mass) => force / mass;  
        double forceOfAcceleration(double mass, double acceleration) => mass * acceleration;
        double accelerationFromDelta(double deltaVelocity, double deltaTime) => deltaVelocity / deltaTime;

        
        // mass init calculate max lean
        int miLastTrajectoryPlane;
        double mdTrajectoryAngle;

        const double deg = Math.PI / 180.0;


        Vector3D up(Vector3D forward, Vector3D left) => forward.Cross(left);
        Vector3D down(Vector3D forward, Vector3D right) => forward.Cross(right);
        Vector3D left(Vector3D forward, Vector3D down) => forward.Cross(down);
        Vector3D right(Vector3D forward, Vector3D up) => forward.Cross(up);
        Vector3D front(Vector3D right, Vector3D down) => right.Cross(down);
        Vector3D back(Vector3D right, Vector3D up) => right.Cross(up);

        void calculateTrajectory(bool aSlow = false) {
            MatrixD mat;
            double ddtMag;

            
            var desiredVelo = 99.8 * MathHelper.Clamp(mdDistance2Objective / mdStopDistance, 0.0, aSlow ? 0.1 : 1.0);
            if (desiredVelo > mdDistance2Objective || mdDistance2Objective < 100) {
                if (desiredVelo > mdDistance2Objective * 0.1) {
                    desiredVelo = mdDistance2Objective * 0.1;
                }
            }
            if (aSlow && desiredVelo > 10.0) {
                desiredVelo = 10.0;
            }
            
            //g.log("calculateTrajectory");
            //g.log("mdStopDistance        ", mdStopDistance);
            //g.log("desiredVelo           ", desiredVelo);
            //g.log("mvLinearVelocity      ", mvLinearVelocity);
            //g.log("mvDirection2Objective ", mvDirection2Objective);
            

            var factor = mdGravity == 0 ? 2.0 : 2.0;

            var desiredVec = (mvLinearVelocity * factor) - (mvDirection2Objective * desiredVelo * factor);

            //g.log("desiredVec", desiredVec);

            // var desiredDampeningThrust = mass * (2 * velocity + gravity);
            //var ddt = mdMass * (2 * mvLinearVelocity + mvGravity);
            // todo double desiredVelo and bring 2 x velo back into equation
            //g.log("mvGravity", mvGravity);
            var ddt = mdMass * (desiredVec + mvGravity);
            var ddtDir = ddt;
            var ddtLenSq = ddt.LengthSquared();

            //g.log("mdGravity ", mdGravity);
            if (mdGravity > 0 && ddtLenSq > mdNewtons * mdNewtons) {
                // ddt is requesting more force than we can apply

                // elfie wolfe says
                // shipVector = shipControllers[0].GetShipVelocities().LinearVelocity;
                // shipToGravityVector = VectorProjection(shipVector, gravityVector);
                // shipToHorizontalVector = (shipVector - shipToGravityVector);

                double v2oDot;
                var v2o = MAF.project(mvLinearVelocity + mvGravity, mvDirection2Objective, out v2oDot);
                var vflat = mvLinearVelocity - v2o;

                ddtDir = mvGravity * mdMass;
                var force = mdNewtons - mdGravity * mdMass;

                ddtDir += vflat * mdMass;
                force -= vflat.Length();

                
                
                var dif = v2oDot - desiredVelo;
                // -9 = 1 - 10 going too slow
                //  9 = 10 - 1 going too fast
                if (dif > 0) {
                    // to fast
                    ddtDir += mvDirection2Objective * force;
                } else {
                    // too slow
                    ddtDir -= mvDirection2Objective * force;
                }
                
                
                ddtMag = mdNewtons;
            } else {
                ddtMag = ddtDir.Normalize();
            }
            var ab = MAF.angleBetween(mRC.WorldMatrix.Down, ddtDir);
            if (mdGravity > 0) {
                if (ab > Math.PI / 4) {
                    double dot;
                    ab = MAF.angleBetween(mRC.WorldMatrix.Up, -mvGravityDirection, out dot);
                    if (dot < 0) {
                        ddtMag = 0;
                    } else {
                        if (ab > Math.PI / 4.0) {
                            ddtMag = 0;
                        } else {
                            ddtMag = forceAtLean(Math.PI / 2.0, ab, mdGravity, mdMass);
                        }
                    }
                }
            } else {
                if (ab > Math.PI / 16) {
                    ddtMag = 0;
                }
            }
            ThrustN(ddtMag);
            mat = mRC.WorldMatrix;

            mGyro.Rotate(ddtDir);
            /*ApplyGyroOverride(
                rotate2direction("Pitch", ddtDir, mat.Right, mat.Up, mat.Down),
                bYawAround ? 0.1 : 0.0,
                rotate2direction("Roll", ddtDir, mat.Forward, mat.Up, mat.Down)
            );*/

        }
        
        



        /// <summary>
        /// returns force needed to maintain a trajectory on a virtual plane
        /// </summary>
        /// <param name="B">virtual plane angle</param>
        /// <param name="C">ship tilt angle</param>
        double forceAtLean(double B, double C, double gravity, double mass) {
            //
            //           C
            //          / \
            //         /   \
            //        /     \
            //       /       \
            //      b         a
            //     /           \
            //    /             \
            //   /               \
            //  A-------c---------B
            //          
            // A nothing
            // B virtual plane angle
            // C thrust deviation from gravity
            // a gravity * mass
            // b desired thrust
            // c nothing
            //var B = Math.Acos(mvDirection2Objective.Dot(mvGravityDirection));
            //var C = Math.Acos(mvGravityDirection.Dot(mRC.WorldMatrix.Down));

            //g.log("forceAtLean");
            //g.log($"B       = {B}");
            //g.log($"C       = {C}");
            //g.log($"gravity = {gravity}");
            //g.log($"mass    = {mass}");

            var A = Math.PI - B - C;
            var b = (gravity * mass) * (Math.Sin(B) / Math.Sin(A));
            return b;
        }
        /*
        void zztrajectory3() {
            
            return;
            g.log("trajectory3");
            MatrixD mat;
            //var desVeloVec = mvDirection2Objective * mdDistance2Objective;

            // 500 / 1000 = 0.5
            // mdStopDistance = (mdLinearVelocity * mdLinearVelocity) / (mdMaxAccel * 2);
            // 500 = v / (mdMaxAccel * 2)
            // d = v / (10 * 2)
            // d = v / 20
            // 10 = v / 20
            // 10 = 200 / 20
            var velo = Math.Sqrt(mdDistance2Objective * (mdMaxAccel * 2));
            //var percent = MathHelper.Clamp(mdDistance2Objective / mdStopDistance, 0.01, 1.0);
            //g.log("percent ", percent);
            //var velo = 99.99 * percent;
            
            
            if (mdDistance2Objective < 20) {
                velo = mdDistance2Objective * 0.1;
                velo = MathHelper.Clamp(velo, 0.1, 1.0);
            } else if (mdDistance2Objective < 50) {
                velo = MathHelper.Clamp(velo, 0.1, 1.0);
            } else if (mdDistance2Objective < 500) {
                velo = MathHelper.Clamp(velo, 0.1, 10.0);
            }
            g.log("velo ", velo);
            //var desiredDampeningThrust = mass * (2 * velocity + gravity);


            var desiredVector = Vector3D.Zero;
            Vector3D desiredDir;
            double desiredMag;
            //if (desiredMag > mdNewtons) {

            // this gives down velocity of 0.5 desiredVector += mdMass * (2 * mvLinearVelocity - mvGravityDirection);
            // this gives up velocity of 0.5 desiredVector += mdMass * (2 * mvLinearVelocity + mvGravityDirection);
            // this gives down velocity of 1.0 desiredVector += mdMass * (1 * mvLinearVelocity - mvGravityDirection);
            // this gives up velocity of 1.0 desiredVector += mdMass * (1 * mvLinearVelocity + mvGravityDirection);
            desiredVector += mdMass * (2 * mvLinearVelocity);
            g.log($"mvGravityDirection.Length() = {mvGravityDirection.Length()}");
            // BAD desiredVector += (mdMass * mvLinearVelocity) / mdTimeFactor;
            // BAD desiredVector += mdMass * (mvLinearVelocity / mdTimeFactor);
            // *** desiredVector += (mdMass * mvLinearVelocity) * mdTimeFactor;
            // *** desiredVector += mdMass * (mvLinearVelocity * mdTimeFactor);

            desiredVector += mdMass * mvGravity;

            //var desAngle = (2 * (mvLinearVelocityDirection - (mvDirection2Objective * velo) + mvGravityDirection));
            desiredDir = -desiredVector;
            desiredMag = desiredDir.Normalize();
            
            mat = mRC.WorldMatrix;

            ThrustN(0);
            //ThrustN(desiredMag);
            
            ApplyGyroOverride(
                //rotate2direction("Pitch", desiredDir, mat.Right, mat.Up, mat.Down),
                rotate2direction("Pitch", -mvGravityDirection, mat.Right, mat.Up, mat.Down),
                0,//rotate2direction("Yaw", front(Vector3D.Right, mvGravityDirection), mat.Right, mat.Up, mat.Down),
                rotate2direction("Roll", -mvGravityDirection, mat.Forward, mat.Up, mat.Down)
            );
            return;
            g.log("prefVelo ", velo);

            //var baseAccel = -(mvGravity * mdMass);
            //var desVeloVec = mvDirection2Objective * velo;

            if (mvGravityDirection.Dot(mvDirection2Objective) > 0) {
                // with gravity

            } else {
                // against gravity
            }

            var thrust = mdNewtons;
            thrust -= mdMass * mdGravity;
            


            //var baseAccel = desVeloVec * mdMass;
            //baseAccel -= mdMass * (2 * mvLinearVelocity + mvGravity);
            //var desAccelMag = baseAccel.Normalize();
            //var baseAccel = desVeloVec * mdMass;
            //baseAccel -= mdMass * (2 * mvLinearVelocity + mvGravity);
            //var desAccelMag = baseAccel.Normalize();




            //
            //           C
            //          / \
            //         /   \
            //        /     \
            //       /       \
            //      b         a
            //     /           \
            //    /             \
            //   /               \
            //  A--------c--------B
            //          
            // A nothing
            // B 90
            // C thrust deviation from gravity
            // a gravity * mass
            // b desired thrust
            // c nothing
            //var ab = Math.Acos(baseAccel.Dot(mRC.WorldMatrix.Up));
            //g.log($"AB {ab}");
            /*if (false && ab > 15.0 * deg) {
                if (mvGravityDirection.Dot(mRC.WorldMatrix.Up) > 0) {
                    g.log("pointing down");
                    ThrustN(0);
                } else {
                    g.log("pointing up");
                    if (mvGravityDirection.Dot(mvLinearVelocityDirection) > 0) {                        
                        g.log("going down");
                        ThrustN(mdNewtons);
                    } else {
                        g.log("going up");
                        ThrustN(0);
                    }
                }
            } else {
                g.log("on target");
                if (mdGravity > 0 && desAccelMag > mdNewtons) {
                    g.log("mitigating required acceleration");
                    mat = MatrixD.CreateFromAxisAngle(mvGravity.Cross(mvDirection2Objective), maxLean);
                    baseAccel = Vector3D.Transform(-mvGravity, mat);
                    ThrustN(mdNewtons);
                } else {
                    g.log("acceptable required acceleration");
                    ThrustN(desAccelMag);
                }
            }

            
            mat = mRC.WorldMatrix;
            ApplyGyroOverride(
                rotate2direction("Pitch", baseAccel, mat.Right, mat.Up, mat.Down),
                0,
                rotate2direction("Roll", baseAccel, mat.Forward, mat.Up, mat.Down)
            );
            
        }*/

        /*void zzaaaaztrajectory2() {
            
            return;
            // virtual
            // var angle = Math.Acos(mvGravityDirection.Dot(mvDirection2Objective));
            // var axis = Vector3D.Normalize(mvDirection2Objective.Cross(mvGravityDirection));
            // var mat = MatrixD.CreateFromAxisAngle(axis, -(angle - Math.PI / 2.0));
            // var virtualNormal = Vector3D.TransformNormal(mvGravityDirection, mat);
            // var virtualPlane = mRC.CenterOfMass + mvGravity * mdMass;


            // thrust at angle
            // 3.141 = ~180
            // 1.570 = ~90
            // 0.785 = ~45
            // A = 180° - B - C
            // B angle


            // b = a·sin(B)/sin(A)
            // b thrust mag
            // a grav mag
            // g.log($"a {mdGravity * mdMass}");
            // g.log($"b {b}");

            //
            //           C
            //          / \
            //         /   \
            //        /     \
            //       /       \
            //      b         a
            //     /           \
            //    /             \
            //   /               \
            //  A-------c---------B
            //          
            // A nothing
            // B virtual plane angle
            // C thrust deviation from gravity
            // a gravity * mass
            // b desired thrust
            // c nothing
            var B = Math.Acos(mvDirection2Objective.Dot(mvGravityDirection));
            var C = Math.Acos(mvGravityDirection.Dot(mRC.WorldMatrix.Down));
            var A = Math.PI - B - C;
            var b = (mdGravity * mdMass) * (Math.Sin(B) / Math.Sin(A));
            ThrustN(b < 0 ? -b : b);
            g.log($"A {A}");
            g.log($"B {B}");
            g.log($"C {C}");

            // desired
            var desVeloVec = mvDirection2Objective * (mdDistance2Objective / 10.0);
            var desAccelVec = desVeloVec - mvLinearVelocity;
            var desAccelDir = desAccelVec;
            var desAccelMag = desAccelDir.Normalize();

            var axis = Vector3D.Normalize(desAccelDir.Cross(mvGravityDirection));

            var desAccelPercent = desAccelMag > mdMaxAccel ? 1.0 : desAccelMag / mdMaxAccel;
            g.log("desAccelMag     ", desAccelMag);
            g.log("desAccelPercent ", desAccelPercent);
            g.log("maxLean         ", maxLean);
            g.log("curLean         ", C);
            var desLeanAngle = maxLean * desAccelPercent;

            if (mvDirection2Objective.Dot(desAccelDir) > 0) {
                B = desLeanAngle;
                g.log("angle pos ", B);
            } else {
                B = -desLeanAngle;
                g.log("angle neg ", B);
            }
            var mat = MatrixD.CreateFromAxisAngle(axis, B);
            var dir = Vector3D.TransformNormal(mvGravityDirection, mat);
            mat = mRC.WorldMatrix;
            ApplyGyroOverride(
                rotate2direction("Pitch", -dir, mat.Right, mat.Up, mat.Down),
                0,
                rotate2direction("Roll", -dir, mat.Forward, mat.Up, mat.Down)
            );


            // sin(θ) = Opposite / Hypotenuse
            // cos(θ) = Adjacent / Hypotenuse
            // tan(θ) = Opposite / Adjacent

            // law of sines a/sin(A)=b/sin(A)=c/sin(C) angles ABC are opposite of sides abc
            // α alpha  
            // β beta
            // γ gamma
            // θ theta

            // cross target x grav

        }*/
        
        double _mdMaxLean = double.NaN;
        
        double maxLean {
            // sin(θ) = Opposite / Hypotenuse
            // cos(θ) = Adjacent / Hypotenuse
            // tan(θ) = Opposite / Adjacent
            get {
                if (mdGravity == 0) {
                    mdGravityLast =
                    _mdMaxLean = 0;
                } else {
                    var calc = false;
                    if (mdMass != mdMassLast) {
                        mdMassLast = mdMass;
                        calc = true;
                    }
                    if (Math.Abs(mdGravity - mdGravityLast) > 0.1) {
                        mdGravityLast = mdGravity;
                        calc = true;
                    }
                    if (mdNewtons != mdNewtonsLast) {
                        mdNewtonsLast = mdNewtons;
                        calc = true;
                    }
                    if (calc) {
                        var adjacent = mdGravity * mdMass;
                        var hypotenuse = mdNewtons;
                        var cos = adjacent / hypotenuse;
                        _mdMaxLean = Math.Acos(cos);
                    }
                }
                return _mdMaxLean;
            }
        }
        
        Vector3D mvDisplacement2Objective;
        Vector3D mvDirection2Objective;
        Vector3D mvObjectiveProjection;
        double mdObjectiveDot;
        double mdTotalMass;
        double mdBaseMass;

        readonly HashSet<long> mKnownPlanets = new HashSet<long>();
        void findPlanet() {
            // pitch = asin(-d.Y);
            // yaw = atan2(d.X, d.Z)
            mPlanet = null;
            mdSeaLevel = 0;
            var target = mRC.CenterOfMass + (mvGravityDirection * (mdAltitudeAbsHighest + (mRC.CubeGrid.WorldAABB.Perimeter / 4.0)));
            var e = new MyDetectedEntityInfo();
            if (scanner.Scan(target, ref e)) {
                if (!processPlanet(e)) {
                    target = mRC.CenterOfMass + ranDir() * 1000.0;
                    if (scanner.Scan(target, ref e)) {
                        processPlanet(e);
                    }
                }
            }
        }

        bool processPlanet(MyDetectedEntityInfo e) {
            if (e.Type == MyDetectedEntityType.Planet) {
                if (!mKnownPlanets.Contains(e.EntityId)) {
                    mKnownPlanets.Add(e.EntityId);
                    //g.persist(g.gps("planet", e.Position));
                }
                mPlanet = e;
                var dir = mRC.CenterOfMass - mPlanet.Value.Position;
                var mag = dir.Normalize();
                mdSeaLevel = mag - mdAltitudeSea;
                return true;
            }
            return false;
        }
        
        double getAltitude(Vector3D aTarget) {
            var result = 0.0;
            if (mPlanet.HasValue) {
                var p = mPlanet.Value;
                var disp = aTarget - p.Position;
                var dir = disp;
                var dist = dir.Normalize();
                result = dist - mdSeaLevel;
            }
            return result;
        }
        double mdSeaLevel;

        void initVelocity() {

            //g.log("rotation time ", mdRotationTime);
            var sm = mRC.CalculateShipMass();
            mdMass = sm.PhysicalMass;
            mdTotalMass = sm.TotalMass;
            mdBaseMass = sm.BaseMass;
            
            // gravity and altitude
            mdAltitudeSurface =
            mdAltitudeSea = 0;
            
            mvGravityDirection =
            mvGravity = mRC.GetNaturalGravity();

            if (!mvGravity.IsZero()) {
                mdGravity = mvGravityDirection.Normalize();

                double e = 0;
                mRC.TryGetPlanetElevation(MyPlanetElevation.Surface, out mdAltitudeSurface);
                mRC.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out mdAltitudeSea);
                
                //g.log("mdAltitudeSea ", mdAltitudeSea);
                //g.log("mdSeaLevel    ", mdSeaLevel);
                //g.log("mdAltitudeSurface ", mdAltitudeSurface);

                if (!mPlanet.HasValue) {
                    findPlanet();
                } else {
                    var pps = new PPS(mPlanet.Value.Position, mdSeaLevel, mRC.CenterOfMass);
                    g.log(g.gps(pps.ToString(), pps.Position));
                }
            } else {
                mdSeaLevel = 
                mdGravity = 0;
                mPlanet = null;
            }
            //g.log("objective altitude ", getAltitude(mMission.Objective));

            mdRawAccel = mdNewtons / mdMass;
            mdMaxAccel = mdRawAccel - mdGravity;
            var minAccel = mdGravity * 2.0;
            //if (mdRawAccel > minAccel) {
                // F=MA
                // A=F/M
                // M=F/A
                // F = 12
                // M =  3
                // A =  4
                // 12=3*4 
                // 4=12/3
                // 3=12/4
                mdAvailableMass = mdNewtons / minAccel;
                mdAvailableMass -= mdMass;
            //} else {
                //mdAvailableMass = 0;
            //}
            var sv = mRC.GetShipVelocities();
            
            mvLinearVelocityDirection = mvLinearVelocity = sv.LinearVelocity;
            mdLinearVelocity = mvLinearVelocityDirection.Normalize();
            mvAngularVelocity = sv.AngularVelocity;
            mdAngularVelocitySquared = mvAngularVelocity.LengthSquared();

            // v10 / 1
            // 100 / 2
            mdStopDistance = ((mdLinearVelocity * mdLinearVelocity) / (mdMaxAccel * 2)) + (mdLinearVelocity * 25.0);
            //mdStopDistance = (10000) / (mdMaxAccel * 2);

            
            //mdStopDistance = 10000.0 / ((mdRawAccel - mdGravity) * 2);
            if (mdGravity == 0) {

                mvDirection2Objective = mvDisplacement2Objective = mMission.Objective - mRC.CenterOfMass;

                
                if (!mvDirection2Objective.IsZero()) {
                    mdDistance2Objective = mvDirection2Objective.Normalize();
                } else {
                    mdDistance2Objective = 0;
                }
                
                
            } else {
                
                var CoM = mRC.CenterOfMass;
                if (mPlanet.HasValue) {
                    //g.log(g.gps("mMission.Objective", mMission.Objective));
                    var alt = getAltitude(mMission.Objective);
                    var dir = Vector3D.Normalize(mRC.CenterOfMass - mPlanet.Value.Position);
                    CoM = mPlanet.Value.Position + (dir * (alt + mdSeaLevel));
                    //g.log(g.gps("CoMCalculated", CoM));
                }
                mvObjectiveProjection = MAF.orthoProject(mMission.Objective, CoM, -mvGravityDirection, out mdObjectiveDot);
                //g.log(g.gps("mvObjectiveProjection", mvObjectiveProjection));
                mvDirection2Objective = Vector3D.Normalize(mvObjectiveProjection - mRC.CenterOfMass);
                mdDistance2Objective = (mvObjectiveProjection - mRC.CenterOfMass).Length();
            }
            

            //g.log("mdDistance2Objective ", mdDistance2Objective);
            //g.log("mdLinearVelocity     ", mdLinearVelocity);
            //g.log("mdAvailableMass      ", mdAvailableMass);

            //g.log("mdMass               ", mdMass);
            //g.log("mdTotalMass          ", mdTotalMass);
            //g.log("mdBaseMass           ", mdBaseMass);

            //g.log("mdObjectiveDot       ", mdObjectiveDot);
            /*
            g.log("mdDistance2Objective ", mdDistance2Objective);
            g.log("mdLinearVelocity     ", mdLinearVelocity);
            g.log("mdStopDistance       ", mdStopDistance);
            g.log("mdRawAccel           ", mdRawAccel);
            g.log("mdMaxAccel           ", mdMaxAccel);
            g.log("mdMass               ", mdMass);
            g.log("mdTotalMass          ", mdTotalMass);
            g.log("mdBaseMass           ", mdBaseMass);
            g.log("mdAvailableMass      ", mdAvailableMass);
            g.log("mdTimeFactor         ", mdTimeFactor);*/

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
                g.persist(ex.ToString());
            }
        }
        void setMissionTest() {
            initMission();
            mMission.Detail = Mission.Details.test;
        }
        int igcMessagesSent = 0;
        int igcMessagesFailed = 0;
        bool mbAutoCharge = false;
        double time = 0;
        readonly Lag lag = new Lag(25);
        void processCommand(string aArgument) {
            try {
                if (null != aArgument) {
                    var args = aArgument.Split(' ');
                    
                    if (0 < args.Length) {
                        switch (args[0]) {
                            case "p":
                                if (args.Length > 1) {
                                    int p;
                                    if (int.TryParse(args[1], out p))
                                        g.removeP(p);
                                } else {
                                    g.removeP(0);
                                }
                                break;
                            case "dock":
                                if (args.Length > 1)
                                    if (!setMissionDock(args[1]))
                                        g.persist($"Dock '{args[1]}' not found.");
                                break;
                            case "damp":
                                setMissionDamp();
                                break;
                            case "patrol":
                                setMissionPatrol();
                                break;
                            case "navigate":
                                if (args.Length > 1)
                                    setMissionNavigate(args[1]);
                                break;
                            case "box":
                                if (args.Length > 1) {
                                    setBoxNavigation(args[1]);
                                }
                                break;
                            case "test":
                                setMissionTest();
                                break;
                            case "depart":
                                undock();
                                break;
                            case "scan":
                                if (args.Length > 1)
                                    setMissionScan(args[1]);
                                break;
                            case "calibrate":
                                setMissionCalibrate();
                                break;
                            case "follow":
                                setMissionFollow();
                                break;
                            case "alti":
                                if (args.Length > 1) {
                                    if (mPlanet.HasValue) {
                                        try {
                                            var pps = new PPS(mPlanet.Value.Position, mdSeaLevel, mRC.CenterOfMass);
                                            pps = new PPS(
                                                mPlanet.Value.Position,
                                                mdSeaLevel,
                                                pps.Azimuth,
                                                pps.Elevation,
                                                double.Parse(args[1])
                                            );
                                            setMissionDamp();
                                            mMission.Objective = pps.Position;
                                        } catch (Exception ex) { g.persist("failed to parse pps"); }
                                    }
                                }
                                break;
                            case "pps":
                                if (args.Length > 3) {
                                    if (mPlanet.HasValue) {
                                        try {
                                            var pps = new PPS(
                                                mPlanet.Value.Position,
                                                mdSeaLevel,
                                                MathHelper.ToRadians(double.Parse(args[1])),
                                                MathHelper.ToRadians(double.Parse(args[2])),
                                                double.Parse(args[3])
                                            );
                                            setMissionDamp();
                                            mMission.Objective = pps.Position;
                                        } catch { g.persist("failed to parse pps"); }
                                    }
                                }
                                break;
                            case "gps2pps":
                                if (args.Length > 1) {
                                    if (mPlanet.HasValue) {
                                        try {
                                            var wp = MyWaypointInfo.Empty;
                                            if (findWaypoint(args[1], ref wp)) {
                                                var pps = new PPS(mPlanet.Value.Position, mdSeaLevel, wp.Coords);
                                                g.persist(pps.ToString());
                                            }
                                        } catch { g.persist("failed to parse pps"); }
                                    }
                                }
                                break;
                            case "pid":
                                if (args.Length > 3) {
                                    try {
                                        var p = double.Parse(args[1]);
                                        var i = double.Parse(args[2]);
                                        var d = double.Parse(args[3]);
                                        mGyro.setPid(p, i, d);
                                        g.persist($"pid {p} {i} {d}");
                                    } catch { g.persist("failed to parse pid"); }
                                }
                                break;
                        }
                    }
                }
            } catch (Exception ex) {
                g.persist(ex.ToString());
            }
        }
        void Main(string argument, UpdateType aUpdate) {
            float value;
            if (float.TryParse(argument, out value)) {
                Echo("Success " + value.ToString());
            }
            var runtimeAverage = lag.update(Runtime.LastRunTimeMs);
            
            time += Runtime.TimeSinceLastRun.TotalSeconds;
            string str;
            if ((aUpdate & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
                processCommand(argument);
            }
            if ((aUpdate & (UpdateType.IGC)) != 0) {
                receiveMessage();
            }
            if ((aUpdate & (UpdateType.Update100)) != 0) {
                autoDock();
            }
            if ((aUpdate & (UpdateType.Update10)) != 0) {
                //g.log("igc success ", igcMessagesSent, " fail ", igcMessagesFailed);
                g.log(runtimeAverage);
                //g.log("Time ", time);

                foreach (var d in mDocks.Values) {
                    g.log("Dock: ", d.Name);
                }
                
                try {
                    initVelocity();
                    if (null != mMission.Connector) {
                        //log.log(gps("Dock Approach", mMission.Connector.Approach));
                        //log.log(gps("Final Approach", mMission.Connector.ApproachFinal));
                        //log.log(gps("Dock Objective", mMission.Connector.Position));
                        //log.log(gps("Dock Direction", mMission.Connector.Position + mMission.Connector.Direction));
                    }
                    doMission();
                    str = g.clear();
                    if (null != mLCD) {
                        mLCD.WriteText(str);
                    }
                } catch (Exception ex) {
                    g.persist(ex.ToString());
                    str = g.clear();
                }

                Echo(str);
            }
        }
        
        void ThrustN(double aNewtons) {

            //g.log("ThrustN requested ", aNewtons, "N");
            //g.log("ThrustN requested ", (aNewtons / mdNewtons) * 100.0, "%");

            var distribution = aNewtons / mThrusters.Count;

            mdNewtons = 0;
            
            foreach (var t in mThrusters) {
                
                var max = t.MaxEffectiveThrust;
                mdNewtons += max;
                if (aNewtons > 0) {
                    var percent = 1.0;
                    if (distribution < max) {
                        percent = distribution / max;
                    }
                    t.Enabled = true;
                    t.ThrustOverridePercentage = (float)percent;
                } else {
                    t.Enabled = false;
                }
            }
            
            //g.log("mdNewtons ", mdNewtons);
            
        }
        void motor2Angle(IMyMotorStator aHinge, float aAngle) {
            if (check(aHinge)) {
                var delta = aAngle - aHinge.Angle;
                g.log("hinge delta", delta);
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
                g.log("check failed");
            }
            return result;
        }
        void pointRotoAtTarget(IMyMotorStator aRoto, Vector3D aTarget) {
            // cos(angle) = dot(vecA, vecB) / (len(vecA)*len(vecB))
            // angle = acos(dot(vecA, vecB) / (len(vecA)*len(vecB)))
            // angle = acos(dot(vecA, vecB) / sqrt(lenSq(vecA)*lenSq(vecB)))
            if (check(aRoto)) {
                if (aTarget == Vector3D.Zero) {
                    pointRotoAtDirection(aRoto, aTarget);
                } else {

                    var matrix = aRoto.WorldMatrix;
                    var projectedTarget = aTarget - Vector3D.Dot(aTarget - matrix.Translation, matrix.Up) * matrix.Up;
                    var projectedDirection = Vector3D.Normalize(matrix.Translation - projectedTarget);
                    pointRotoAtDirection(aRoto, projectedDirection);
                }
            }
        }
        void pointRotoAtDirection(IMyMotorStator aRoto, Vector3D aDirection) {
            if (null != aRoto) {
                if (Vector3D.Zero == aDirection) {
                    aRoto.TargetVelocityRad = 0;
                    return;
                }
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
                    /*
                    if (v > 0) {
                        if (v < 0.01) {
                            v = 0.0;
                        }
                    } else {
                        if (v > 0.01) {
                            v = 0.0;
                        }
                    }*/
                } else {
                    g.log("angle nan");
                }
                aRoto.Enabled = true;
                aRoto.RotorLock = false;
                aRoto.TargetVelocityRad = (float)(v * 6.0);
            }
        }


        

        

        
        enum DampStep : int
        {
            stop,
            hold
        }

        readonly Vector3D[] mvaCorners = new Vector3D[8];
        readonly Dictionary<long, Connector> mDocks = new Dictionary<long, Connector>();


        double mdAltitudeAbsLowest => Math.Abs(mdAltitudeSea) < Math.Abs(mdAltitudeSurface) ? Math.Abs(mdAltitudeSea) : Math.Abs(mdAltitudeSurface);
        double mdAltitudeAbsHighest => Math.Abs(mdAltitudeSea) > Math.Abs(mdAltitudeSurface) ? Math.Abs(mdAltitudeSea) : Math.Abs(mdAltitudeSurface);

        double mdAltitudeLowest => mdAltitudeSea < mdAltitudeSurface ? mdAltitudeSea : mdAltitudeSurface;
        double mdAltitudeHighest => mdAltitudeSea > mdAltitudeSurface ? mdAltitudeSea : mdAltitudeSurface;

        double mdAltitudeSurface;
        double mdAltitudeSea;
        
        double mdLinearVelocity = 0.0;
        double mdAngularVelocitySquared = 0.0;

        Vector3D mvGravity;
        Vector3D mvGravityDirection;

        double mdMass;
        double mdMassLast;

        double mdNewtons;
        double mdNewtonsLast;

        double mdGravity;
        double mdGravityLast;

        //double mdAcceleration;
        double mdStopDistance;
        double mdDistance2Objective;
        
        double mdRawAccel;
        double mdMaxAccel;
        
        
        const double mdRotateEpsilon = 0.01 * deg;
        

        readonly GTS mGTS;
        readonly Logger g;
        readonly Gyro mGyro;

        readonly List<IMyThrust> mThrusters = new List<IMyThrust>();
        //readonly List<IMyGyro> mGyros = new List<IMyGyro>();
        readonly List<IMyBatteryBlock> mBatteries = new List<IMyBatteryBlock>();
        readonly List<IMyGasTank> mFuelTanks = new List<IMyGasTank>();
        readonly List<MyDetectedEntityInfo> mDetected =new List<MyDetectedEntityInfo>();
        
        readonly HashSet<long> mDetectedIds = new HashSet<long>();

        Mission mMission;

        IMyTextPanel mLCD;
        readonly IMyBroadcastListener mListener;
        IMyRemoteControl mRC;
        

        
        IMyShipConnector mCon;
        IMySensorBlock mSensor;

        int miDock = 0;
        //int miCount = 0;
        const int miInterval = 10;
        const double mdTickTime = 1.0 / 60.0;
        const double mdTimeFactor = mdTickTime * miInterval;
        Scanner scanner;        
        
        Vector3D mvLinearVelocity = Vector3D.Zero;
        Vector3D mvAngularVelocity = Vector3D.Zero;
        Vector3D mvLinearVelocityDirection = Vector3D.Zero;

    }

    // large connectors distance apart 2.65 
    // small connector distance from large 1.85
    // small connector distance from small 1.00

}
