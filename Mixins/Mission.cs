using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
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
        double mPreferredVelocity = 1;
        public bool Complete { get; private set; }
        bool onDestination;
        /// <summary>
        /// true when our orbital maneuver result is an escape teajectory this is needed because
        /// the clustering action may result in a sphere center moving from in front of us to behind us
        /// </summary>
        bool onEscape;
        //Vector3D lastOrbital;

        //double calcRadius = 0;
        public Mission(ShipControllerModule aController, ThyDetectedEntityInfo aDestination) {
            ctr = aController;
            mDestination = aDestination;
            if (ctr.LinearVelocity > 1) {
                mPreferredVelocity = ctr.LinearVelocity;
            }
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

        HashSet<long> mEscapeSet = new HashSet<long>();
        List<Vector3D> mEscape = new List<Vector3D>();
        /// <summary>
        /// returns desired direction of travel
        /// </summary>
        /// <returns></returns>
        Vector3D collisionDetect() {
            

            var wv = ctr.Grid.WorldVolume;


            //ctr.logger.log("mObstacle.Radius ", mObstacle.Radius);
            //var meToDest = Vector3D.Normalize(mDestination.Position - wv.Center);
            //var meToObst = Vector3D.Normalize(mObstacle.Center - wv.Center);
            //var meToDest = mDestination.Position - wv.Center;
            //var meToObst = mObstacle.Center - wv.Center;
            //var dot = meToDest.Dot(meToObst);
            //ctr.logger.log("dot ", dot);

            var scanDist = wv.Radius + (ctr.Thrust.StopDistance * 3) + PADDING;

            //ctr.logger.log("base scan dist ", scanDist);
            
            var detected = false;
            for (int i = 0; i < 5; i++) {
                var lvd = ctr.LinearVelocityDirection;

                if (ctr.LinearVelocity < 5.0) {
                    switch (MAF.random.Next(0, 6)) {
                        case 0: lvd = ModuleManager.WorldMatrix.Forward; break;
                        case 1: lvd = ModuleManager.WorldMatrix.Backward; break;
                        case 2: lvd = ModuleManager.WorldMatrix.Left; break;
                        case 3: lvd = ModuleManager.WorldMatrix.Right; break;
                        case 4: lvd = ModuleManager.WorldMatrix.Up; break;
                        case 5: lvd = ModuleManager.WorldMatrix.Down; break;
                    }
                }
                Vector3D scanPoint;
                if (true || i == 0) {
                    scanPoint = wv.Center + MAF.ranDir() * (wv.Radius * 5);
                }  else {
                    scanPoint = wv.Center + lvd * scanDist;
                }
                //scanPoint = wv.Center + lvd * scanDist;
                // create a point for scan sphere based on our velocity

                // random dir around point to scan
                var rd = MAF.ranDir();
                // keep scans on the far side of the sphere
                if (rd.Dot(lvd) < 0) {
                    rd = -rd;
                }
                // random distance from sphere center to scan
                var scanRadius = 0.1 + 2 * wv.Radius * MAF.random.NextDouble();
                //ctr.logger.log("ship wv radius ", wv.Radius);
                //ctr.logger.log("base scan distance ", scanDist);

                scanPoint += rd * scanRadius;
                //ctr.logger.log(ctr.logger.gps("scanPoint", scanPoint));


                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (ctr.Camera.Scan(scanPoint, out entity, out thy)) {
                    if ((thy != null && thy != mDestination) || (thy == null && entity.EntityId != 0 && entity.Type != MyDetectedEntityType.CharacterHuman)) {
                        //ctr.logger.persist("Avoiding a " + entity.Name);
                        detected = true;
                        BoundingSphereD sphere;
                        if (onEscape) {
                            var dispFromThing = wv.Center - entity.HitPosition.Value;
                            var lengthFromThing = dispFromThing.Length();
                            var inverseDistFromThing = 1 / lengthFromThing;
                            mEscape.Add(dispFromThing * inverseDistFromThing);
                        }
                        if (thy == null) {
                            sphere = BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox);
                        } else {
                            sphere = thy.WorldVolume;
                        }
                        if (mObstacle.Radius == 0) {
                            mObstacle = sphere;
                        } else {
                            mObstacle = mObstacle.Include(sphere);
                        }
                    }
                }
            }
            if (detected) {
                mPreferredVelocity *= 0.99;
            } else {
                mPreferredVelocity *= 1.1;
                mObstacle.Radius *= 0.99;
                if (mObstacle.Radius < 1) {
                    mObstacle.Radius = 0;
                }
            }
            mPreferredVelocity = MathHelperD.Clamp(mPreferredVelocity, 1.0, 50.0);
            var result = Vector3D.Zero;
            if (mObstacle.Radius > 0) {
                bool wasOnEscape = onEscape;
                onEscape = false;
                var orbitalResult = orbitalManeuver(wv, mObstacle);
                if (orbitalResult == Vector3D.Zero) {
                    mEscapeSet.Clear();
                    mEscape.Clear();
                    mObstacle.Radius = 0;
                } else {
                    if (!wasOnEscape && onEscape) {
                        mEscape.Add(-ctr.LinearVelocityDirection);
                        mEscape.Add(orbitalResult);
                    }
                    if (onEscape) {
                        foreach(var e in mEscape) {
                            result += e;
                        }
                        result.Normalize();
                    } else {
                        result = orbitalResult;
                    }
                }
                
            }
            return result;
        }

        
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
            
            //ctr.logger.log("dist ", dist);
            ctr.logger.log("Estimated arrival ", (dist / ctr.LinearVelocity) / 60.0, " minutes");
            //preferredVelocity = MathHelperD.Clamp(dist / ctr.Thrust.FullStop, 0, 1.0) * preferredVelocity;
            var preferredVelocity = MathHelperD.Clamp((dist / 2) / ctr.Thrust.StopDistance, 0, 1.0) * mPreferredVelocity;

            //ctr.logger.log("preferredVelocity ", preferredVelocity);
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
