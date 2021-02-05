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

        const double MAX_VELO = 100.0;
        const double MIN_VELO = 0.03;
        void rotate2vector(Vector3D aTarget) {
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
                if (0.9 > t.FilledRatio) {
                    return true;
                }
            }
            return false;
        }
        bool fullCharge() {
            foreach (var b in mBatteries) {
                if (b.CurrentStoredPower / b.MaxStoredPower < 0.95) {
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
                //rpm = rps2rpm(angle);
                rpm = angle;
            }
            //g.log("rotate2direction", aGyroOverride, " ", rpm);
            //mGyro.GyroOverride = true;
            //mGyro.SetValueFloat(aGyroOverride.ToString(), (float)rpm);
            //log(aGyroOverride, " rpm ", rpm);
            return rpm;
        }
        double rps2rpm(double rps) => (rps / (Math.PI * 2)) * 60.0;
        double rpm2rps(double rpm) => (rpm * (Math.PI * 2)) / 60.0;
        double angleBetween(Vector3D a, Vector3D b) {
            var dot = a.Dot(b);
            if (dot < -1.0) {
                dot = -1.0;
            } else if (dot > 1.0) {
                dot = 1.0;
            }
            var result = Math.Acos(dot);
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
                g.log("BAD ", x);
            } else if (x < -1.0) {
                g.log("BAD ", x);
            }
            var result = Math.Asin(x) / 2.0 / Math.PI * 60.0 / 0.166666;
            if (Math.Abs(result) < 0.001) {
                //result = 0.0;
            }
            if (result > 60.0) {
                g.log("dying ", result);
                //Me.Enabled = false;
            }
            return result * scale;
        }

        void setMissionDamp() {
            initMission();
            mMission.Objective = mRC.CenterOfMass;
        }
        
        void doMissionDamp() {
            switch (mCon.Status) {
                case MyShipConnectorStatus.Connectable:
                    ThrustN(mdMass * mdGravity);
                    break;
                case MyShipConnectorStatus.Connected:
                    ThrustN(0);
                    rotate2vector(Vector3D.Zero);
                    break;
                case MyShipConnectorStatus.Unconnected:
                    trajectory(mMission.Objective);
                    break;
            }
            
        }
        bool hasCamera => mCamera != null;
        BodyMap map;

        void dock() {
            setBatteryCharge(ChargeMode.Recharge);
            setRefuel(true);
            setGyrosEnabled(false);
            ThrustN(0);
        }
        void undock() {
            setBatteryCharge(ChargeMode.Discharge);
            setRefuel(false);
            setGyrosEnabled(true);
            mCon.Enabled = false;
        }
        void setMissionNavigate(string aWaypointName) {
            MyWaypointInfo waypoint = MyWaypointInfo.Empty;
            if (findWaypoint(aWaypointName, ref waypoint)) {
                initMission();
                undock();
                mMission.Detail = Mission.Details.navigate;
                setMissionObjective(waypoint.Coords);

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

        //Whip's ApplyGyroOverride Method v10 - 8/19/17
        void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed) {
            var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs 
            
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, mRC.WorldMatrix);
            //g.log("ApplyGyroOverride");
            foreach (var thisGyro in mGyros) {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                //g.log("transformedRotationVec", transformedRotationVec);
                thisGyro.GyroOverride = true;
                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
            }
        }
        void aimCameraDir(Vector3D aDirection) {
            foreach (var r in mCameraRot) {
                pointRotoAtDirection(r, aDirection);
            }
        }
        void aimCameraTgt(Vector3D aTarget) {
            foreach (var r in mCameraRot) {
                pointRotoAtTarget(r, aTarget);
            }
            g.log("aimed camera");
        }
        int scanNavCount = 0;
        BoundingBoxD moveTowards(BoundingBoxD aBox, Vector3D aDir) {
            var disp = aBox.Max - aBox.Min;
            var mag = disp.Length();
            return new BoundingBoxD(aBox.Min + (aDir * mag), aBox.Max + (aDir * mag));
        }
        readonly HashSet<Vector3L> mVisits;
        void setMissionScan(string aWaypointName) {
            if (hasCamera) {
                var wp = MyWaypointInfo.Empty;
                if (findWaypoint(aWaypointName, ref wp)) {
                    initMission();
                    mVisits.Clear();

                    scanNavCount = 0;
                    mCamera.Enabled = true;
                    mCamera.EnableRaycast = true;
                    foreach (var r in mCameraRot) {
                        r.RotorLock = false;
                        r.Enabled = true;
                    }
                    mMission.Detail = Mission.Details.scan;
                    
                    // mvMissionTranslation is used as the original location to scan in
                    mMission.Translation = wp.Coords;
                    
                    // will modify mMission.Objective to navigate in the scan zone as necessary
                    mMission.Objective = mRC.CenterOfMass;
                    mbScanGood = Me.CubeGrid.WorldAABB;
                    
                } else {
                    g.persist("waypoint not found");
                }
            } else {
                g.persist("Camera required for scan mission.");
            }
        }
        int miScanStep;
        bool mbScanComplete = true;
        BoundingBoxD mbScan;
        void setScan(BoundingBoxD aBox) {
            miScanStep = -1;
            mbScanComplete = false;
            aBox.Inflate(10);
            mbScan = aBox;
            mDetected.Clear();
            aBox.GetCorners(mvaCorners);
        }
        void doScan() {
            g.log("camera range ", mCamera.AvailableScanRange);
            g.log("mbScanComplete ", mbScanComplete);
            g.log("miScanStep ", miScanStep);
            if (!mbScanComplete) {
                if (miScanStep == -1) {
                    if (_doScan(mbScan.Center)) {
                        miScanStep++;
                    }
                }
                if (miScanStep > -1 && miScanStep < 8) {
                    if (_doScan(mvaCorners[miScanStep])) {
                        miScanStep++;
                        if (miScanStep == 8) {
                            mbScanComplete = true;
                            g.log("ZEROING CQAMERA");
                            aimCameraTgt(Vector3D.Zero);
                        }
                    }
                }
            }
        }
        bool _doScan(Vector3D aTarget) {
            var result = false;
            var dir = aTarget - mCamera.WorldMatrix.Translation;
            var dist = dir.Normalize() + (mbScan.Max - mbScan.Min).Length();
            var ab = angleBetween(mCamera.WorldMatrix.Forward, dir);
            aimCameraTgt(aTarget);
            if (ab < 1.5) {
                if (mCamera.AvailableScanRange > dist) {
                    aTarget += dir * dist;
                    
                    var e = mCamera.Raycast(aTarget);
                    if (e.Type == MyDetectedEntityType.None) {
                        result = true;
                    } else {
                        bool use = true;
                        if (e.EntityId == Me.CubeGrid.EntityId) {
                            use = false;
                        } else {
                            foreach (var cg in mCameraRot) {
                                if (cg.CubeGrid.EntityId == e.EntityId) {
                                    use = false;
                                    break;
                                }
                            }
                        }
                        if (!mDetectedIds.Contains(e.EntityId)) {
                            //g.persist(e);
                            mDetectedIds.Add(e.EntityId);
                        }
                        if (use) {
                            result = true;
                            mDetected.Add(e);
                        }
                    }
                }
            }
            return result;
        }
        
        BoundingBoxD c2direction(Vector3D aDirection) {
            var cbox = box.c(mRC.WorldMatrix.Translation);
            IMyTerminalBlock b;
            return box.c(cbox.Center + (aDirection * box.cdist));
            
        }
        
        readonly Random r = new Random(9);
        Vector3D rv() => Vector3D.Normalize(new Vector3D(r.NextDouble() - 0.5, r.NextDouble() - 0.5, r.NextDouble() - 0.5));
        BoundingBoxD mbScanGood;
        void doMissionScan() {
            // mvMissionTranslation is used as the original location to scan in
            // will modify mMission.Objective to navigate in the scan zone as necessary

            // mMission.Step
            // 0 = scan to direction of mvMissionTranslation
            // 1 = 0 found collision, if in gravity check above, in space check around
            // 2 = 1 found collision, work through 26 directions, miMissionSubStep
            // 3 = 2 found collision, work through random directions
            // 4 = ??
            bool detected = false;

            if (mDetected.Count > 1) {
                // detected
                detected = true;
                mMission.Step++;
                if (2 == mMission.Step) {
                    mMission.SubStep = 0;
                } else if (4 == mMission.Step) {
                    mMission.Step = 0;
                }
            } else if (mbScanComplete && mbScan.Center != Vector3D.Zero) {
                mMission.Step = 0;
                mbScanGood = mbScan;
                mMission.Objective = mbScan.Center;
                scanNavCount++;
                g.persist(g.gps($"nav{scanNavCount}", mbScan.Center));
            }

            if (mbScanComplete && 0 == mMission.Step) {
                setScan(moveTowards(mbScanGood, Vector3D.Normalize(mMission.Translation - mRC.CenterOfMass)));
            } else if (mbScanComplete && 1 == mMission.Step) {
                var dir = (Vector3D.Normalize(mMission.Translation - mRC.CenterOfMass) + mvGravityDirection) * 0.5;
                setScan(moveTowards(mbScanGood, dir));
            } else if (mbScanComplete && 2 == mMission.Step) {
                Vector3D dir = Vector3D.Zero;
                switch (mMission.SubStep) {
                    case 0: dir = (Vector3D.Forward + Vector3D.Up) / 2.0; break;
                    case 1: dir = (Vector3D.Forward + Vector3D.Up + Vector3D.Right) / 3.0; break;
                    case 2: dir = (Vector3D.Up + Vector3D.Right) / 2.0; break;
                    case 3: dir = (Vector3D.Backward + Vector3D.Up + Vector3D.Right) / 3.0; break;
                    case 4: dir = (Vector3D.Backward + Vector3D.Up) / 2.0; break;
                    case 5: dir = (Vector3D.Backward + Vector3D.Up + Vector3D.Left) / 3.0; break;
                    case 6: dir = (Vector3D.Up + Vector3D.Left) / 2.0; break;
                    case 7: dir = (Vector3D.Forward + Vector3D.Up + Vector3D.Left) / 3.0; break;
                    case 8: dir = Vector3D.Up; break;

                    case 9: dir = (Vector3D.Forward + Vector3D.Down) / 2.0; break;
                    case 10: dir = (Vector3D.Forward + Vector3D.Down + Vector3D.Right) / 3.0; break;
                    case 11: dir = (Vector3D.Down + Vector3D.Right) / 2.0; break;
                    case 12: dir = (Vector3D.Backward + Vector3D.Down + Vector3D.Right) / 3.0; break;
                    case 13: dir = (Vector3D.Backward + Vector3D.Down) / 2.0; break;
                    case 14: dir = (Vector3D.Backward + Vector3D.Down + Vector3D.Left) / 3.0; break;
                    case 15: dir = (Vector3D.Down + Vector3D.Left) / 2.0; break;
                    case 16: dir = (Vector3D.Forward + Vector3D.Down + Vector3D.Left) / 3.0; break;
                    case 17: dir = Vector3D.Down; break;

                    case 18: dir = Vector3D.Forward; break;
                    case 19: dir = (Vector3D.Forward + Vector3D.Right) / 2.0; break;
                    case 20: dir = Vector3D.Right; break;
                    case 21: dir = (Vector3D.Backward + Vector3D.Right) / 2.0; break;
                    case 22: dir = Vector3D.Backward; break;
                    case 23: dir = (Vector3D.Backward + Vector3D.Left) / 2.0; break;
                    case 24: dir = Vector3D.Left; break;
                    case 25: dir = (Vector3D.Forward + Vector3D.Left) / 2.0; break;
                }
                mMission.SubStep++;
                if (mMission.SubStep == 26) {
                    mMission.Step++;
                }
                setScan(moveTowards(mbScanGood, dir));
            } else if (mbScanComplete) {
                setScan(moveTowards(mbScanGood, rv()));
            }
            g.log("mMission.Step ", mMission.Step);
            g.log("mMission.SubStep ", mMission.SubStep);
            doScan();
            trajectory(mMission.Objective);
            
        }
        void zdoMissionScan() {
            // mvMissionTranslation is used as the original location to scan in
            // will modify mMission.Objective to navigate in the scan zone as necessary

            // mSensor.DetectedEntities(mDetected);
            
            var detected = false;
            g.log("mMission.Step ", mMission.Step);

            for (int i = mDetected.Count - 1; i > -1 && !detected; i--) {
                var e = mDetected[i];
                mDetected.RemoveAt(i);
                detected = true;  
            }

            if (detected) {
                if (mMission.Step == 0) {
                    mMission.Step = 2;
                } else if (mMission.Step == 8) {
                    mMission.Step = 0;
                } else {
                    mMission.Step++;
                }
            } else {
            }
            Vector3D dir = Vector3D.Zero;
            switch (mMission.Step) {
                case 0: // scan
                case 1: // navigate
                    dir = Vector3D.Normalize(mMission.Translation - mMission.Objective);
                    break;
                case 2: //
                    if (mdGravity > 0) {
                        dir = (Vector3D.Normalize(mMission.Translation - mMission.Objective) + -mvGravityDirection) * 0.5;
                    }
                    break;
                case 3: // up
                    dir = Vector3D.Up;
                    break;
                case 4: // down
                    dir = Vector3D.Down;
                    break;
                case 5: // forward
                    dir = Vector3D.Forward;
                    break;
                case 6: // backward
                    dir = Vector3D.Backward;
                    break;
                case 7: // check left
                    dir = Vector3D.Left;
                    break;
                case 8: // check right
                    dir = Vector3D.Right;
                    break;
            }

            if (mbScanComplete) {
                var cbox = box.c(mRC.CenterOfMass);
                if (mMission.Step > 2 && !detected) {
                    //g.persist(gps($"nav {++scanNavCount}", mvScan));
                    mMission.Step = 1;
                }
                if (mMission.Step == 1 && cbox.Center == box.c(mMission.Objective).Center) {
                    mMission.Step = 0;
                }
                if (mMission.Step > 2) {
                    var newBox = box.c(cbox.Center + dir * box.cdist);
                    if (newBox.Center == cbox.Center) {
                        if (mMission.Step == 0) {
                            mMission.Step = 2;
                        } else if (mMission.Step == 8) {
                            mMission.Step = 0;
                        } else {
                            mMission.Step++;
                        }
                    } else {
                    }
                }
                    
            }
            doScan();

            // mvMissionTranslation is used as the original location to scan in
            // will modify mMission.Objective to navigate in the scan zone as necessary

            trajectory(mMission.Objective);
        }
        
        
        void setMissionObjective(Vector3D aObjective) {
            mMission.Objective = aObjective;
            mMission.Distance = (mMission.Objective - mMission.Start).Length();
        }
        void initMission() {
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

            setGyrosEnabled(true);
            setGyrosOverride(false);
            mMission.Connector = null;

            if (mCon.Status == MyShipConnectorStatus.Unconnected) mCon.Enabled = false;
            undock();
            if (mCamera != null) 
            mCamera.Enabled = false;
            foreach (var r in mCameraRot) {
                r.RotorLock = true;
                r.Enabled = false;
            }
            
        }
        void setGyrosOverride(bool aValue) {
            foreach (var g in mGyros) {
                g.GyroOverride = aValue;
            }
        }
        void setGyrosEnabled(bool aValue) {
            foreach (var g in mGyros) {
                g.Enabled = aValue;
            }
        }

        void doMission() {
            g.log("doMission ", mMission.Detail);
            switch (mMission.Detail) {
                case Mission.Details.damp:
                    trajectory(mMission.Objective);
                    break;
                case Mission.Details.navigate:
                    missionNavigate();
                    break;
                case Mission.Details.dock:
                    doMissionDock();
                    break;
                case Mission.Details.patrol:
                    if (0 == mMission.Step) {
                        setMissionObjective(PATROL_0);
                    } else {
                        setMissionObjective(PATROL_1);
                    }
                    if (mdDistance2Objective < 10.0) {
                        mMission.Step++;
                    }
                    if (mMission.Step > 1) {
                        mMission.Step = 0;
                    }
                    missionNavigate();
                    break;
                case Mission.Details.test:
                    //mGyro.SetValueFloat("Pitch", 10.0f);
                    var c = findConnector("con1");
                    var local = -world2pos(mRC.CenterOfMass, mCon.WorldMatrix);
                    //var objective = local2pos(local, c.World) + (c.World.Forward * 2.65);
                    //log.log(gps("test con1", objective));
                    break;
                case Mission.Details.scan:
                    doMissionScan();
                    break;
                default:
                    g.log("mission unhandled");
                    break;
            }
        }
        Connector findConnector(string aConnector) {
            g.log("findConnector");
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
                var projectedPosition = reject(mRC.CenterOfMass, approachPlane, mMission.Connector.Direction);
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
                    setMissionObjective(mMission.Connector.Approach);
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
                    setMissionObjective(mMission.Connector.ApproachFinal);
                    msg = "rendezvous with final approach";
                    var precision = 10.0;
                    if (DockStep.depart == step) {
                        msg = "depart dock area";
                        precision = 100.0;
                    }
                    // goto beginning of final approach
                    
                    if (precision > missionNavigate()) {
                        mMission.Step++;
                    }
                    break;
                case DockStep.dock:
                    setMissionObjective(mMission.Connector.Objective);
                    msg = "rendezvous with dock";
                    d = missionNavigate();
                    
                    if (d < 5.0) {
                        mCon.Enabled = true;
                        mMission.Step = (int)DockStep.connect;
                        if (0 == mdGravity) {
                            rotate2vector(Vector3D.Zero);
                            ThrustN(0);
                        }
                    }
                    break;
                case DockStep.connect:
                    msg = "connecting to dock";
                    switch (mCon.Status) {
                        case MyShipConnectorStatus.Connectable:
                            ThrustN(mdMass * mdGravity);
                            rotate2vector(Vector3D.Zero);
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
                                rotate2vector(mRC.CenterOfMass + (mMission.Connector.Direction * 500.0));
                            } else {
                                missionNavigate();
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
                    if (lowBattery() || lowFuel()) {
                        mMission.Step = (int)DockStep.charging;
                    } else {
                        mMission.Step = (int)DockStep.depart;
                        mbAutoCharge = false;
                        undock();
                    }
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
        bool moon = false;
        double missionNavigate(bool aGyroHold = false) {
            /*
            var displacement2objective = mMission.Objective - mvMissionStart;
            var displacement2start = mvMissionStart - mMission.Objective;
            var missionDistance = displacement2objective.Length();
            var dir2objective = displacement2objective / missionDistance;
            var dir2start = dir2objective * -1.0;
            var dist2objectiveFromShip = (mMission.Objective - mvCoM).Length();
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

            trajectory(mMission.Objective, aGyroHold: aGyroHold);
            var result = mdDistance2Objective;
            
            if (result < 0.5) {
                result = 0.0;
                
            }
            g.log("navigate result ", result);
            return result;

            //rotate2vector(Vector3D.Zero);

        }
        double distance2objective() => _distance2(mMission.Objective, mRC.CenterOfMass);
        double _distance2(Vector3D aTarget, Vector3D aOrigin) => (aTarget - aOrigin).Length();
        
        double thrustPercent(Vector3D aDirection, Vector3D aNormal) {
            var result = 0.0;
            var offset = angleBetween(aDirection, aNormal);
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
            mThrusters = new List<IMyThrust>();
            mGyros = new List<IMyGyro>();
            mBatteries = new List<IMyBatteryBlock>();
            mFuelTanks = new List<IMyGasTank>();
            mDetected = new List<MyDetectedEntityInfo>();
            mCameraRot = new List<IMyMotorStator>();
            mDetectedIds = new HashSet<long>();
            mvaCorners = new Vector3D[8];
            mVisits = new HashSet<Vector3L>();

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
            
            mGyros.Clear();
            mGTS.initList(mGyros);


            if (mGTS.get(ref mCamera)) {
                mCamera.Enabled = false;
            }

            mGTS.initList(mThrusters);
            mGTS.initList(mBatteries);
            mGTS.initList(mFuelTanks);
            mGTS.initListByTag("camera", mCameraRot);

            for (int i = mFuelTanks.Count - 1; i > -1; i--) {
                /*MyObjectBuilder_OxygenTank / LargeHydrogenTank
                MyObjectBuilder_OxygenTank / LargeHydrogenTankSmall
                MyObjectBuilder_OxygenTank / OxygenTankSmall
                MyObjectBuilder_OxygenTank / SmallHydrogenTank
                MyObjectBuilder_OxygenTank / SmallHydrogenTankSmall*/
                var t = mFuelTanks[i];
                var n = t.BlockDefinition.SubtypeName;
                if ("SmallHydrogenTank" == n) {
                } else {
                    mFuelTanks.RemoveAt(i);
                    g.persist(n);
                }
            }
            initMission();
            ThrustN(0);
            initVelocity();
            
            
            foreach (var g in mGyros) {
                g.Enabled = true;
                g.SetValueFloat("Yaw", 0);
                g.SetValueFloat("Pitch", 0);
                g.SetValueFloat("Roll", 0);
            }
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
        
        void trajectory2() {
            var angle = Math.Acos(mvDirection2Objective.Dot(mvGravityDirection));
            var axis = Vector3D.Normalize(mvDirection2Objective.Cross(mvGravityDirection));
            var mat = MatrixD.CreateFromAxisAngle(axis, -(angle - Math.PI / 2.0));
            var virtualNormal = Vector3D.TransformNormal(mvGravityDirection, mat);
            var virtualPlane = mRC.CenterOfMass + mvGravity * mdMass;


            // thrust at angle
            var ab = Math.Acos(mvGravityDirection.Dot(mRC.WorldMatrix.Down));
            // A = 180° - B - C
            // B angle
            // C = ab
            var A = Math.PI - angle - ab;
            // b = a·sin(B)/sin(A)
            // b thrust mag
            // a grav mag
            var b = (mdGravity * mdMass) * (Math.Sin(angle) / Math.Sin(A));
            g.log($"C {ab}");
            g.log($"a {mdGravity * mdMass}");
            g.log($"B {angle}");
            g.log($"A {A}");
            g.log($"b {b}");
            ThrustN(b);


            // desired
            var desVeloVec = mvDirection2Objective * (mdDistance2Objective / 100.0);
            var desAccelVec = desVeloVec - mvLinearVelocity;
            var desAccelDir = desAccelVec;
            var desAccelMag = desAccelDir.Normalize();


            axis = Vector3D.Normalize(desAccelDir.Cross(mvGravityDirection));

            if (mvDirection2Objective.Dot(desAccelDir) > 0) {
                angle = 0.1;
            } else {
                angle = -0.1;
            }
            g.log("angle ", angle);
            mat = MatrixD.CreateFromAxisAngle(axis, angle);
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

        }
        void trajectory(Vector3D aObjective, bool aGyroHold = false) {
            trajectory2();
            return;

            // g.log("stopDist ", stopDist);
            // 2 = 1000 / 500
            // 0.8 = 400 / 500 eighty percent "throttle"
            

            //var veloVec = mvLinearVelocity - (mvDirection2Objective * (99.0 * velopct));
            
            // desired
            var desVeloVec = mvDirection2Objective * 1.0;
            var desVeloDir = mvDirection2Objective;

            var desAccelVec = desVeloVec - mvLinearVelocity;
            var desAccelDir = desAccelVec;
            var desAccelMag = desAccelDir.Normalize();

            //* If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
            //* compute the dot product v . (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
            var thrust = 0.0;
            if (mvGravityDirection.Dot(desAccelDir) < 0) {
                // target above
                var desAccelPercent = desAccelMag > mdMaxAccel ? 1.0 : desAccelMag / mdMaxAccel;
                var desLeanAngle = maxLean * desAccelPercent;
            } else {
                // target below
                //desiredVelocityDirection = -desiredVelocityDirection;
                
            }
            //var veloMag = veloVec.Length();
            //var veloDir = veloVec / veloMag;
            //veloVec = veloDir * 100.0;
            // var ddt = mdMass * (2 * veloVec + mvGravity);
            ///g.log("ddt", ddt);
            // var mag = ddt.Length();
            // var dir = ddt / mag;
            var m = mRC.WorldMatrix;

            if (aGyroHold) {
                rotate2vector(Vector3D.Zero);
            } else {
                /*ApplyGyroOverride(
                    rotate2direction("Pitch", desiredVelocityDirection, m.Right, m.Up, m.Down),
                    0,
                    rotate2direction("Roll", desiredVelocityDirection, m.Forward, m.Up, m.Down)
                );*/
            }

            //ThrustN(mag * thrustPercent(-dir, mRC.WorldMatrix.Up));
            ThrustN(thrust);
        }
        /*void foo() {
        
            // whip says
            // var desiredDampeningThrust = mass * (2 * velocity + gravity);
            //  (ddt / mass) - grav = 2x velo
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
            var prefVelo = targetMag / 500;
            
            if (prefVelo > 1.0) {
                prefVelo = 1.0;
            }
            //var maxvelo = 60.0;
            var maxvelo = mdPreferredVelocity;
            prefVelo *= maxvelo;
            if (targetMag < 1.0 && mRC.GetNaturalGravity().LengthSquared() == 0) {
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

             //* If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
             //* compute the dot product v . (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
             

            
            
            //var up = Vector3D.Normalize(mRC.GetNaturalGravity() * -1);
            //var dot = up.Dot(Vector3D.Normalize(mvCoM - missionMiddle));
            // norm = dirToShipFromMiddle cross directionToObjectiveFromMiddle
            // dot = dir to obj dot norm
            // var norm = Vector3D.Normalize(aDirection.Cross(matrix.Forward));
            // var dot = matrix.Up.Dot(norm);

            //var projection = project(mvCoM, missionMiddle, up);
            //var distFromPlane = (mvCoM - projection).Length();
            //var missionRadius = missionDistance * 0.5;
            //var distance2projection = (projection - missionMiddle).Length();

            ///log("remainingDistance ", remainingDistance);
            //log("distFromPlane ", distFromPlane);
            //if (dot > 0) {
                //log("above plane");
            //} else {
                //log("below plane");
            //}
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

            
             //Try Update10, and apply force of 
            //(max_acceleration / (velocity * 3.0)) * max_force
            //Where the 3 is 
            //0.5 * (60 / update_every_x_ticks) -> 0.5 is a scaling factor of hom smooth the slowing down should be to prevent overshooting
            



            //Plugin in your current speed into u, and 0 into v
            //Use the formula without t, as that is unknown
            // me try 

            var m = mRC.WorldMatrix;
            
            
                // pitch right up down
                // roll forward up down
                //rotate2direction("Pitch", requiredDir, m.Right, m.Up, m.Down);
                rotate2direction("Pitch", extDir, m.Right, m.Up, m.Down);
                //rotate2direction("Roll", requiredDir, m.Forward, m.Up, m.Down);
                rotate2direction("Roll", extDir, m.Forward, m.Up, m.Down);
            
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
        }*/
        double _mdMaxLean = double.NaN;
        double maxLean {
            // sin(θ) = Opposite / Hypotenuse
            // cos(θ) = Adjacent / Hypotenuse
            // tan(θ) = Opposite / Adjacent
            get {

                var calc = false;
                if (mdMass != mdMassLast) {
                    mdMassLast = mdMass;
                    calc = true;
                }
                if (mdGravity != mdGravityLast) {
                    mdGravityLast = mdGravity;
                    calc = true;
                }
                if (mdNewtons != mdNewtonsLast) {
                    mdNewtonsLast = mdNewtons;
                    calc = true;
                }
                if (mdGravity == 0) {
                    _mdMaxLean = double.NaN;
                } else if (calc) {
                    var adjacent = mdGravity * mdMass;
                    var hypotenuse = mdNewtons;
                    var cos = adjacent / hypotenuse;
                    _mdMaxLean = Math.Acos(cos);
                }
                return _mdMaxLean;
            }
        }
        
        Vector3D mvDisplacement2Objective;
        Vector3D mvDirection2Objective;
        
        void initVelocity() {
            mdMass = mRC.CalculateShipMass().PhysicalMass;
            
            // gravity and altitude
            mdAltitude = double.NaN;
            
            mvGravityDirection =
            mvGravity = mRC.GetNaturalGravity();
            
            if (!mvGravity.IsZero()) {
                mdGravity = mvGravityDirection.Normalize();
                
                g.log("mdGravity ", mdGravity.ToString());
                double e;
                mRC.TryGetPlanetElevation(MyPlanetElevation.Surface, out mdAltitude);
                mRC.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out e);
                if (e < mdAltitude) {
                    mdAltitude = e;
                }
            }
            mdRawAccel = mdNewtons / mdMass;
            mdMaxAccel = mdRawAccel - mdGravity;
            var minAccel = mdGravity * 2.0;
            if (mdRawAccel > minAccel) {
                // F=MA
                // A=F/M
                // M=F/A
                // 12=3*4
                // 4=12/3
                // 3=12/4
                mdAvailableMass = mdNewtons / minAccel;
                mdAvailableMass -= mdMass;
            } else {
                mdAvailableMass = 0;
            }
            var sv = mRC.GetShipVelocities();
            
            mvLinearVelocityDirection = mvLinearVelocity = sv.LinearVelocity;
            mdLinearVelocity = mvLinearVelocityDirection.Normalize();

            mdStopDistance = (mdLinearVelocity * mdLinearVelocity) / ((mdRawAccel - mdGravity) * 2);

            mvDirection2Objective = mvDisplacement2Objective = mMission.Objective - mRC.CenterOfMass;
            mdDistance2Objective = mvDirection2Objective.Normalize();
            

            g.log("mdDistance2Objective ", mdDistance2Objective);
            g.log("mdLinearVelocity     ", mdLinearVelocity);
            g.log("mdStopDistance       ", mdStopDistance);
            g.log("mdRawAccel           ", mdRawAccel);
            g.log("mdMaxAccel           ", mdMaxAccel);
            g.log("mdMass               ", mdMass);
            g.log("mdAvailableMass      ", mdAvailableMass);

        }

        
        
        void receiveMessage() {
            try {
                while (mListener.HasPendingMessage) {
                    var msg = mListener.AcceptMessage();
                    switch (msg.Tag) {
                        case "docks":
                            //Me.CustomData += msg.Data.ToString() + Environment.NewLine;
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
        void Main(string argument, UpdateType aUpdate) {
            string str;
            if ((aUpdate & (UpdateType.Terminal)) != 0) {
                try {
                    if (null != argument) {
                        var args = argument.Split(' ');
                        if (0 < args.Length) {
                            switch (args[0]) {
                                case "p":
                                    if (1 < args.Length) {
                                        int p;
                                        if (int.TryParse(args[1], out p)) g.removeP(p);
                                    } else {
                                        g.removeP(0);
                                    }
                                    break;
                                case "dock":
                                    if (1 < args.Length) if (!setMissionDock(args[1])) g.persist($"Dock '{args[1]}' not found.");
                                    break;
                                case "damp":
                                    setMissionDamp();
                                    break;
                                case "patrol":
                                    setMissionPatrol();
                                    break;
                                case "navigate":
                                    if (args.Length > 1) setMissionNavigate(args[1]);
                                    break;
                                case "test":
                                    setMissionTest();
                                    break;
                                    break;
                                case "depart":
                                    undock();
                                    break;
                                case "scan":
                                    if (args.Length > 1) setMissionScan(args[1]);
                                    break;
                            }
                        }
                    }
                } catch (Exception ex) {
                    g.persist(ex.ToString());
                }
            }
            if ((aUpdate & (UpdateType.IGC)) != 0) {
                receiveMessage();
            }
            if ((aUpdate & (UpdateType.Update100)) != 0) {
                autoDock();
            }
            if ((aUpdate & (UpdateType.Update10)) != 0) {
                //g.log("igc success ", igcMessagesSent, " fail ", igcMessagesFailed);
                foreach (var d in mDocks.Values) {
                    g.log("Dock: ", d.Name);
                }
                try {
                    //initSensor();
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
        void ThrustN(double aNewtons) => ThrustN((float)aNewtons);
        void ThrustN(float aNewtons) {
            float fMax, fPercent;
            
            mdNewtons = 0;
            g.log("ThrustN requested ", aNewtons, "N");
            foreach (var t in mThrusters) {
                fMax = t.MaxEffectiveThrust;
                mdNewtons += fMax;
                if (aNewtons > 0) {
                    if (aNewtons > fMax) {
                        fPercent = 1;
                        aNewtons -= fMax;
                    } else {
                        fPercent = aNewtons / fMax;
                        aNewtons = 0;
                    }
                    t.Enabled = true;
                    t.ThrustOverridePercentage = fPercent;
                } else {
                    t.Enabled = false;
                }
            }
            
            g.log("mdNewtons ", mdNewtons);
            
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
                    g.log("angle nan");
                }
                aRoto.Enabled = true;
                aRoto.RotorLock = false;
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

        

        
        enum DampStep : int
        {
            stop,
            hold
        }

        readonly Vector3D[] mvaCorners;
        readonly Dictionary<long, Connector> mDocks = new Dictionary<long, Connector>();
        

        double mdAltitude;
        
        double mdLinearVelocity = 0.0;
        double mdAngularVelocity = 0.0;

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
        
        
        const double mdRotateEpsilon = 0.01;
        

        readonly GTS mGTS;
        readonly Logger g;

        readonly List<IMyThrust> mThrusters;
        readonly List<IMyGyro> mGyros;
        readonly List<IMyBatteryBlock> mBatteries;
        readonly List<IMyGasTank> mFuelTanks;
        readonly List<MyDetectedEntityInfo> mDetected;
        readonly List<IMyMotorStator> mCameraRot;

        readonly HashSet<long> mDetectedIds;

        Mission mMission;

        IMyTextPanel mLCD;
        readonly IMyBroadcastListener mListener;
        IMyRemoteControl mRC;
        

        IMyCameraBlock mCamera;
        IMyShipConnector mCon;


        int miDock = 0;
        //int miCount = 0;
        const int miInterval = 10;
        const double mdTickTime = 1.0 / 60.0;
        const double mdTimeFactor = mdTickTime * miInterval;
        


        

        
        Vector3D PATROL_0 = new Vector3D(45519.94, 164664.93, -85803.92);
        Vector3D PATROL_1 = new Vector3D(46015.06, 164066.36, -86526.14);
        Vector3D MOON_DOCK = new Vector3D(19706.77, 143964.78, -109088.83);

        
        //Vector3D mvCoM;

        
        Vector3D mvLinearVelocity = Vector3D.Zero;
        //Vector3D mvAngularVelocity = Vector3D.Zero;
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
