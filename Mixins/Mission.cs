﻿using Sandbox.Definitions;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Mission {
        const int PADDING = 10;

        readonly ShipControllerModule ctr;
        readonly ThyDetectedEntityInfo mDestination;
        
        Vector3D mTarget;
        BoundingSphereD mObstacle;
        int scansPerTick = 6;

        double veloFact = 0.1;

        public bool Complete { get; private set; }
        bool onDestination = false;
        double calcRadius = 0;
        public Mission(ShipControllerModule aController, ThyDetectedEntityInfo aDestination) {
            ctr = aController;
            mDestination = aDestination;
            calcRadius = aDestination.WorldVolume.Radius;
            calculateTarget(aDestination.WorldVolume, true);
        }
        void calculateTarget(BoundingSphereD sphere, bool isDestination) {
            onDestination = isDestination;
            var wv = ctr.Grid.WorldVolume;
            var padding = sphere.Radius + wv.Radius + PADDING;
            // ctr.logger.persist("padding " + padding);
            
            if (ctr.LinearVelocity < 0.1) {
                var norm = Vector3D.CalculatePerpendicularVector(Vector3D.Normalize(wv.Center - sphere.Center)); // direction from thing
                mTarget = sphere.Center + (norm * padding);
                
            } else {
                var norm = Vector3D.Normalize(sphere.Center - wv.Center); // direction to thing
                var veloNorm = Vector3D.Normalize(ctr.ShipVelocities.LinearVelocity);
                var rejection = Vector3D.Normalize(MAF.reject(veloNorm, norm));
                mTarget = sphere.Center + (rejection * padding);
            }
            //ctr.logger.persist(ctr.logger.gps("Calc", mTarget));
        }
        
        void collisionDetect() {
            var lvd = ctr.LinearVelocityDirection;
            if (!lvd.IsValid()) {
                ModuleManager.Program.Me.Enabled = false;
                throw new Exception("51");
            }
            if (lvd.LengthSquared() < 1.0) {
                switch (MAF.random.Next(0, 6)) {
                    case 0: lvd = ModuleManager.WorldMatrix.Forward; break;
                    case 1: lvd = ModuleManager.WorldMatrix.Backward; break;
                    case 2: lvd = ModuleManager.WorldMatrix.Left; break;
                    case 3: lvd = ModuleManager.WorldMatrix.Right; break;
                    case 4: lvd = ModuleManager.WorldMatrix.Up; break;
                    case 5: lvd = ModuleManager.WorldMatrix.Down; break;
                }
            }
            if (!lvd.IsValid()) {
                ModuleManager.Program.Me.Enabled = false;
                throw new Exception("64");
            }
            var wv = ctr.Grid.WorldVolume;
            bool scanOkay = true;
            bool needCalc = false;
            if (mObstacle.Radius > 0) {
                var meToDest = mDestination.Position - wv.Center;
                var meToObst = mObstacle.Center - wv.Center;
                
                if (meToDest.Dot(meToObst) < 0) {
                    ctr.logger.persist("CLEARED");
                    mObstacle.Radius = 0;
                    needCalc = true;
                }
            }
            if (double.IsNaN(ctr.Thrust.StopDistance) || double.IsInfinity(ctr.Thrust.StopDistance)) {
                ModuleManager.Program.Me.Enabled = false;
                throw new Exception("80");
            }
            var scanDist = ((wv.Radius * 8) + (ctr.Thrust.StopDistance * 2) + PADDING);
            if (double.IsNaN(scanDist) || double.IsInfinity(scanDist)) {
                ModuleManager.Program.Me.Enabled = false;
                throw new Exception("84");
            }
            ctr.logger.log("base scan dist ", scanDist);
            var scanPoint = wv.Center + lvd * scanDist;
            for (int i = 0; i < 2; i++) {
                // create a point for scan sphere based on our velocity
                
                // random dir around point to scan
                var rd = MAF.ranDir();
                // keep scans on the far side of the sphere
                if (rd.Dot(lvd) < 0) {
                    rd = -rd;
                }
                // random distance from sphere center to scan
                var scanRadius = 1 + wv.Radius * MAF.random.NextDouble();
                //ctr.logger.log("base scan radius ", scanDist);
                
                scanPoint += rd * scanRadius;
                //ctr.logger.log(ctr.logger.gps("scanPoint", scanPoint));


                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (ctr.Camera.Scan(scanPoint, out entity, out thy)) {
                    if ((thy != null && thy != mDestination) || (thy == null && entity.EntityId != 0 && entity.Type != MyDetectedEntityType.CharacterHuman)) {
                        BoundingSphereD sphere;
                        if (thy == null) {
                            sphere = BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox);
                        } else {
                            sphere = thy.WorldVolume;
                            sphere = sphere.Include(BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox));
                        }
                        ctr.logger.persist("Detected a thing " + entity.EntityId);
                        if (mObstacle.Radius == 0) {
                            mObstacle = sphere;
                            ctr.logger.persist("Setting new " + sphere.Radius);
                            needCalc = true;
                            break;
                        } else {
                            switch(mObstacle.Contains(sphere)) {
                                case ContainmentType.Disjoint:
                                    mObstacle = sphere;
                                    ctr.logger.persist("Replacing " + sphere.Radius);
                                    needCalc = true;
                                    break;
                                    break;
                                case ContainmentType.Intersects:
                                    mObstacle = mObstacle.Include(sphere);
                                    ctr.logger.persist("Including " + mObstacle.Radius);
                                    needCalc = true;
                                    break;
                                    break;
                                case ContainmentType.Contains:
                                    ctr.logger.persist("Contains " + mObstacle.Radius);
                                    break;
                                default:
                                    ctr.logger.persist("Unknown containment type");
                                    break;
                            }
                        }
                    }
                } else {
                    //ctr.logger.persist("Scan fail");
                    scanOkay = false;
                }
            }
            if (scanOkay) {
                veloFact *= 1.1;
            } else {
                veloFact *= 0.9;
            }
            veloFact = MathHelper.Clamp(veloFact, 0.01, 1.0);
            if (needCalc || (mObstacle.Radius == 0 && mDestination.WorldVolume.Radius != calcRadius)) {
                if (mObstacle.Radius == 0) {
                    calcRadius = mDestination.WorldVolume.Radius;
                    calculateTarget(mDestination.WorldVolume, true);
                } else {
                    calculateTarget(mObstacle, false);
                }
            }
        }
        
        
        public void Update() {
    
            collisionDetect();
            var m = ModuleManager.WorldMatrix;

            //var targetWorld = MAF.local2pos(target, ModuleManager.WorldMatrix);
            //ModuleManager.logger.persist(ModuleManager.logger.gps("targetWorld", targetWorld));

            

            var dir = MAF.world2pos(mTarget, ModuleManager.WorldMatrix);

            var dist = dir.Normalize();
            if (!onDestination) {
                dist = (mDestination.Position - ModuleManager.WorldMatrix.Translation).Length();
            }
            ctr.logger.log("dist ", dist);
            var velocityVec = ctr.LocalLinearVelo;
            var preferredVelocity = 100.0;

            preferredVelocity = MathHelperD.Clamp(dist / ctr.Thrust.FullStop, 0, 1.0) * preferredVelocity;
            ctr.logger.log("preferredVelocity ", preferredVelocity);
            var preferredVelocityVector = dir * preferredVelocity;
            var veloReq = preferredVelocityVector - velocityVec;
            var vrsq = veloReq.LengthSquared();

            if (!ctr.Damp) {
                
                var disp = mTarget - ModuleManager.WorldMatrix.Translation;
                if (onDestination && disp.LengthSquared() < 1) {
                    ctr.Thrust.Acceleration = Vector3D.Zero;
                    Complete = 
                    ctr.Damp = true;
                } else if (vrsq < 25.0) {
                    ctr.Thrust.Acceleration = veloReq;
                    ctr.Gyro.SetTargetPosition(mDestination.Position);
                } else {
                    ctr.Thrust.Acceleration = veloReq;
                    ctr.Gyro.SetTargetPosition(mTarget);
                }
            }
            
            
            if (scansPerTick < 100 && ModuleManager.Lag < 1.0) {
                scansPerTick++;
            } else if (scansPerTick > 6 && ModuleManager.Lag > 1.5) {
                scansPerTick--;
            }
            
            ctr.logger.log($"Scans Per Tick {scansPerTick}");

        }


        VectorHandler CamJob(Vector3D aTarget) {
            float angle = 0;
            var l = ModuleManager.logger;
            
            int count = 0;
            return null;// () => {
                /*
                var dir = axis.Cross(Vector3D.Up);
                if (dir.IsZero()) {
                    dir = axis.Cross(Vector3D.Right);
                }
                angle++;
                angle = MathHelper.WrapAngle(angle);
                var m = MatrixD.CreateFromAxisAngle(axis, angle);
                dir = Vector3D.Transform(dir, m);
                var v = aTarget + (dir * (1 + (MAF.random.NextDouble() * ctr.Grid.WorldVolume.Radius)));
                l.persist(l.gps(count.ToString(), v));
                count++;
                return v;
                */
            

        }

        public enum Details
        {
            damp,
            navigate,
            dock,
            patrol,
            test,
            thrust,
            rotate,
            map,
            scan,
            calibrate,
            follow,
            boxnav,
            none
        }

        
    }
}
