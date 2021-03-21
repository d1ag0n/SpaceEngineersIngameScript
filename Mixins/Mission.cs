﻿using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class Mission {
        const int PADDING = 20;
        
        readonly ShipControllerModule ctr;
        readonly ThyDetectedEntityInfo mDestination;

        Vector3D mTargetDirection;
        BoundingSphereD mObstacle;
        //int scansPerTick = 6;

        double veloFact = 0.1;

        public bool Complete { get; private set; }
        bool onDestination;
        /// <summary>
        /// true when our orbital maneuver result is an escape teajectory this is needed because
        /// the clustering action may result in a sphere center moving from in front of us to behind us
        /// </summary>
        bool onEscape;
        Vector3D lastOrbital;

        //double calcRadius = 0;
        public Mission(ShipControllerModule aController, ThyDetectedEntityInfo aDestination) {
            ctr = aController;
            mDestination = aDestination;
            //calcRadius = aDestination.WorldVolume.Radius;
            //calculateTarget(aDestination.WorldVolume, true);
        }

        /*
        Vector3D calculateTarget(BoundingSphereD sphere, bool isDestination) {
            onDestination = isDestination;
            var wv = ctr.Grid.WorldVolume;
            var padding = sphere.Radius + wv.Radius + PADDING;
            // ctr.logger.persist("padding " + padding);
            if (isDestination) {
                var norm = Vector3D.Normalize(wv.Center - sphere.Center); // direction from thing
                mTarget = sphere.Center + norm * padding;
            } else  if (ctr.LinearVelocity < 0.1) {
                var norm = Vector3D.CalculatePerpendicularVector(Vector3D.Normalize(wv.Center - sphere.Center)); // direction from thing
                mTarget = sphere.Center + (norm * padding);
                
            } else {
                var norm = Vector3D.Normalize(sphere.Center - wv.Center); // direction to thing
                var veloNorm = Vector3D.Normalize(ctr.ShipVelocities.LinearVelocity);
                var rejection = Vector3D.Normalize(MAF.reject(veloNorm, norm));
                mTarget = sphere.Center + (rejection * padding);
            }
            //ctr.logger.persist(ctr.logger.gps("Calc", mTarget));
        }*/

        Vector3D orbitalManeuver(BoundingSphereD aShip, BoundingSphereD aObstacle) {
            var result = Vector3D.Zero;
            var dispFromDestToShip = aShip.Center - mDestination.Position;              // displacement from destination to ship
            var dirFromDestToShip = dispFromDestToShip;                                 // direction from destination to ship
            var distDestToShip = dirFromDestToShip.Normalize();                         // length of displacement from destination to ship
            var rayFromDestToShip = new RayD(mDestination.Position, dirFromDestToShip); // ray from destination to ship            
            var rayIntersect = rayFromDestToShip.Intersects(aObstacle);                 // calculate intersect
            if (rayIntersect.HasValue) {
                Vector3D exitOrbit;                                                     // position where we exit orbit
                if (rayIntersect.Value == 0) {
                    // ray originates inside of obstacle
                    // calculate exit point "above" destination
                    aObstacle = aObstacle.Include(mDestination.WorldVolume);
                    var dispFromObstToDest = mDestination.Position - aObstacle.Center;
                    var dirFromObstToDest = Vector3D.Normalize(dispFromObstToDest);
                    exitOrbit = aObstacle.Center + dirFromObstToDest * (aObstacle.Radius + aShip.Radius + PADDING);
                } else {
                    exitOrbit = mDestination.Position + dirFromDestToShip * (rayIntersect.Value + aShip.Radius + PADDING);
                }
                var distToExit = (exitOrbit - aShip.Center).LengthSquared();
                if (distToExit > (ctr.Thrust.StopDistance * ctr.Thrust.StopDistance)) {
                    var dispFromObstToShip = aShip.Center - aObstacle.Center;
                    var dirFromObstToShip = dispFromObstToShip;
                    var distFromObstToShip = dirFromObstToShip.Normalize();
                    var minOrbitalDist = (aObstacle.Radius + aShip.Radius + PADDING);

                    var minOrbitalPlane = aObstacle.Center + dirFromObstToShip * minOrbitalDist;
                    
                    if ((aObstacle.Center - aShip.Center).LengthSquared() < (minOrbitalDist * minOrbitalDist)) {
                        result = dirFromObstToShip;
                        onEscape = true;
                    } else {
                        var orbitalPlane = aObstacle.Center + dirFromObstToShip * minOrbitalDist;
                        var exitProjection = MAF.orthoProject(exitOrbit, orbitalPlane, dirFromObstToShip);
                        result = Vector3D.Normalize(exitProjection - orbitalPlane);
                    }
                }
            }
            return result;
        }

        //Vector3D calculateTarget(Vector3D aShip, Vector3D aThing) => Vector3D.Normalize(aThing - aShip);

        /// <summary>
        /// returns desired direction of travel
        /// </summary>
        /// <returns></returns>
        Vector3D collisionDetect() {
            

            var wv = ctr.Grid.WorldVolume;


            ctr.logger.log("mObstacle.Radius ", mObstacle.Radius);
            //var meToDest = Vector3D.Normalize(mDestination.Position - wv.Center);
            //var meToObst = Vector3D.Normalize(mObstacle.Center - wv.Center);
            var meToDest = mDestination.Position - wv.Center;
            var meToObst = mObstacle.Center - wv.Center;
            var dot = meToDest.Dot(meToObst);
            ctr.logger.log("dot ", dot);

            var scanDist = wv.Radius + (ctr.Thrust.StopDistance * 2) + PADDING;

            ctr.logger.log("base scan dist ", scanDist);
            
            var detected = false;
            for (int i = 0; i < 8; i++) {
                var lvd = ctr.LinearVelocityDirection;

                if (lvd.LengthSquared() < 25.0) {
                    switch (MAF.random.Next(0, 6)) {
                        case 0: lvd = ModuleManager.WorldMatrix.Forward; break;
                        case 1: lvd = ModuleManager.WorldMatrix.Backward; break;
                        case 2: lvd = ModuleManager.WorldMatrix.Left; break;
                        case 3: lvd = ModuleManager.WorldMatrix.Right; break;
                        case 4: lvd = ModuleManager.WorldMatrix.Up; break;
                        case 5: lvd = ModuleManager.WorldMatrix.Down; break;
                    }
                }
                var scanPoint = wv.Center + lvd * scanDist;

                // create a point for scan sphere based on our velocity

                // random dir around point to scan
                var rd = MAF.ranDir();
                // keep scans on the far side of the sphere
                if (rd.Dot(lvd) < 0) {
                    rd = -rd;
                }
                // random distance from sphere center to scan
                var scanRadius = 2.0 + ((wv.Radius * 3) * MAF.random.NextDouble());
                //ctr.logger.log("base scan radius ", scanDist);

                scanPoint += rd * scanRadius;
                //ctr.logger.log(ctr.logger.gps("scanPoint", scanPoint));


                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (ctr.Camera.Scan(scanPoint, out entity, out thy)) {
                    if ((thy != null && thy != mDestination) || (thy == null && entity.EntityId != 0 && entity.Type != MyDetectedEntityType.CharacterHuman)) {
                        detected = true;
                        BoundingSphereD sphere;
                        if (onEscape) {
                            var dispFromThing = wv.Center - entity.Position;
                            var dirFromThing = Vector3D.Normalize(dispFromThing);
                            lastOrbital = Vector3D.Normalize(dirFromThing + lastOrbital);
                        } else {
                            if (thy == null) {
                                sphere = BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox);
                            } else {
                                sphere = thy.WorldVolume;
                                //sphere = sphere.Include(BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox));
                            }
                            //ctr.logger.persist("Detected a thing " + entity.EntityId);
                            if (mObstacle.Radius == 0) {
                                mObstacle = sphere;
                                //ctr.logger.persist("Setting new " + sphere.Radius);
                                break;
                            } else {
                                mObstacle = mObstacle.Include(sphere);
                                switch (mObstacle.Contains(sphere)) {
                                    case ContainmentType.Disjoint:
                                        //mObstacle = sphere;
                                        //ctr.logger.persist("Replacing " + sphere.Radius);
                                        break;
                                    case ContainmentType.Intersects:

                                        //ctr.logger.persist("Including " + mObstacle.Radius);
                                        break;
                                }
                            }
                        }

                    }
                }
            }
            if (detected) {
                mPreferredVelocity *= 0.9;
            } else {
                mPreferredVelocity *= 1.1;
                mObstacle.Radius *= 0.99;
                if (mObstacle.Radius < 1) {
                    mObstacle.Radius = 0;
                }
            }
            mPreferredVelocity = MathHelperD.Clamp(mPreferredVelocity, 1.0, 100.0);
            
            if (mObstacle.Radius > 0) {
                bool wasOnEscape = onEscape;
                onEscape = false;
                var orbitalResult = orbitalManeuver(wv, mObstacle);
                if (wasOnEscape && onEscape) {
                    orbitalResult = lastOrbital;
                }
                lastOrbital = orbitalResult;
                if (lastOrbital.IsZero()) {
                    //result = calculateTarget(wv.Center, mDestination.Position);
                    //onDestination = true;
                    //mObstacle.Radius = 0;
                    ModuleManager.logger.log("Direct in progress");
                } else {
                    //onDestination = false;
                    ModuleManager.logger.log("Orbital in progress");
                }
            } else {
                //result = calculateTarget(wv.Center, mDestination.Position);
                //onDestination = true;
                ModuleManager.logger.log("Direct in progress");
            }
            return lastOrbital;
        }

        double mPreferredVelocity = 1;
        public void Update() {
            var wv = ctr.Grid.WorldVolume;
            var m = ModuleManager.WorldMatrix;
            var worldDir = collisionDetect();

            var dispFromDestToShip = wv.Center - mDestination.Position;
            var dirFromDestToShip = dispFromDestToShip;
            var distFromDestToShip = dirFromDestToShip.Normalize();
            var stop = mDestination.Position + dirFromDestToShip * (mDestination.WorldVolume.Radius + wv.Radius + PADDING);
            var stopDisp = stop - wv.Center;
            var stopDir = stopDisp;
            var dist = stopDir.Normalize();
            if (worldDir.IsZero()) {
                worldDir = stopDir;
            }
            var localDir = MAF.world2dir(worldDir, m);
            var llv = ctr.LocalLinearVelo;
            
            ctr.logger.log("dist ", dist);
            //preferredVelocity = MathHelperD.Clamp(dist / ctr.Thrust.FullStop, 0, 1.0) * preferredVelocity;
            var preferredVelocity = MathHelperD.Clamp((dist / 2) / ctr.Thrust.StopDistance, 0, 1.0) * mPreferredVelocity;

            ctr.logger.log("preferredVelocity ", preferredVelocity);
            var preferredVelocityVector = localDir * preferredVelocity;
            var accelReq = 2 * (preferredVelocityVector - llv);

            //var vrsq = veloReq.LengthSquared();
            
            if (!ctr.Damp) {
                if (dist < 1) {
                    ctr.Gyro.SetTargetDirection(Vector3D.Normalize(mDestination.Position - ctr.Grid.WorldVolume.Center));
                    ctr.Thrust.Acceleration = Vector3D.Zero;
                    Complete =
                    ctr.Damp = true;
                } else if (dist < 20) {
                    ctr.Thrust.Acceleration = accelReq;
                    ctr.Gyro.SetTargetDirection(Vector3D.Normalize(mDestination.Position - ctr.Grid.WorldVolume.Center));
                } else { 
                    ctr.Thrust.Acceleration = accelReq;
                    ctr.Gyro.SetTargetDirection(ctr.LinearVelocityDirection);
                }
            }
            
            

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
