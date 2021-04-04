using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public abstract class MissionBase {

        readonly HashSet<long> mEscapeSet = new HashSet<long>();
        readonly List<Vector3D> mEscape = new List<Vector3D>();

        double mPrefVelo;
        Vector3D mStop;
        Vector3D mMaxAccel;
        double mStopLengthSquared;
        bool onEscape;
        BoundingSphereD mObstacle;
        protected IMyTerminalBlock NavBlock;
        double BaseVelocityLength;
        Vector3D _BaseVelocity;
        Vector3D BaseVelocityDirection;
        protected Vector3D BaseVelocity {
            get {
                return _BaseVelocity;
            }
            set {
                if (_BaseVelocity != value) {
                    _BaseVelocity = value;
                    if (value.IsZero()) {
                        BaseVelocityLength = 0.0;
                        BaseVelocityDirection = Vector3D.Zero;
                    } else {
                        BaseVelocityDirection = value;
                        BaseVelocityLength = BaseVelocityDirection.Normalize();
                    }
                }
            }
        }

        protected readonly ShipControllerModule ctr;
        protected BoundingSphereD mDestination;
        protected readonly ThyDetectedEntityInfo mEntity;

        protected double PADDING = 10.0;
        protected double MAXVELO = 80.0;
        protected double mPreferredVelocityFactor = 1;
        protected double mMaxAccelLength;
        protected double Altitude;
        protected Vector3D Target;
        protected Vector3D mDirToDest;
        protected double mDistToDest;

        protected BoundingSphereD Volume => mEntity == null ? mDestination : mEntity.WorldVolume;
        public bool Complete { get; protected set; }
        public MissionBase(ShipControllerModule aController, BoundingSphereD aDestination) {
            ctr = aController;
            mDestination = aDestination;
        }
        public MissionBase(ShipControllerModule aController, ThyDetectedEntityInfo aEntity) {
            ctr = aController;
            mEntity = aEntity;
        }

        public virtual void Update() {
            var wv = ctr.Grid.WorldVolume;
            if (NavBlock != null) {
                wv.Center = NavBlock.WorldMatrix.Translation;
            }
            mMaxAccel = ctr.Thrust.MaxAccel(ctr.LocalLinearVelo);
            mMaxAccelLength = mMaxAccel.Length();
            mStop = ctr.Thrust.Stop(mMaxAccel);
            mStopLengthSquared = mStop.LengthSquared();
            if (double.IsNaN(mStopLengthSquared)) {
                mStop.X = mStop.Y = mStop.Z = mStopLengthSquared = double.PositiveInfinity;
            }

            //ctr.logger.log("mStopLength ", mStopLength);
            var dispToDest = Volume.Center - wv.Center;
            mDirToDest = dispToDest;
            mDistToDest = mDirToDest.Normalize();
            //ctr.logger.log("mDistToDest ", mDistToDest);
            if (Volume.Radius > 0) {
                Altitude = wv.Radius + Volume.Radius + PADDING;
                Target = Volume.Center + -mDirToDest * Altitude;
                dispToDest = Target - wv.Center;
                mDirToDest = dispToDest;
                mDistToDest = mDirToDest.Normalize();
            }
            //ctr.logger.log("BaseVelocityLength ", BaseVelocityLength);

            mPrefVelo = ctr.Thrust.PreferredVelocity(mMaxAccelLength, mDistToDest);
            //ctr.logger.log("mPrefVelo ", mPrefVelo);
            //mPrefVelo += BaseVelocityLength;

            //ctr.logger.log("mPrefVelo ", mPrefVelo);

            if (ctr.LinearVelocity == 0) {
                mPrefVelo = 1.0;
            } else {
                if (mPrefVelo > mDistToDest) {
                    mPrefVelo = mDistToDest;
                }
            }
        }

        Vector3D orbitalManeuver(BoundingSphereD aShip, BoundingSphereD aObstacle) {
            var result = Vector3D.Zero;
                
            // todo can probably use ortho project here
            var rayFromDestToShip = new RayD(Volume.Center, -mDirToDest);               // ray from destination to ship            
            var rayIntersect = rayFromDestToShip.Intersects(aObstacle);                 // calculate intersect
            if (rayIntersect.HasValue) {
                Vector3D exitOrbit;                                                     // position where we exit orbit

                if (rayIntersect.Value == 0) {
                    // ray originates inside of obstacle
                    // calculate exit point "above" destination
                    aObstacle = aObstacle.Include(Volume);
                    var dispFromObstToDest = Volume.Center - aObstacle.Center;
                    var dirFromObstToDest = Vector3D.Normalize(dispFromObstToDest);
                    exitOrbit = aObstacle.Center + dirFromObstToDest * (aObstacle.Radius + aShip.Radius + PADDING);
                } else {
                    // todo use ortho project?
                    exitOrbit = Volume.Center + (-mDirToDest) * (rayIntersect.Value + aShip.Radius + PADDING);
                }
                var distToExit = (exitOrbit - aShip.Center).LengthSquared();

                var maxAccel = ctr.Thrust.MaxAccel(ctr.LocalLinearVelo);
                if (distToExit > mStopLengthSquared) {
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
        Vector3D collisionDetect() {
            var wv = ctr.Grid.WorldVolume;
            var stopLen = Math.Sqrt(mStopLengthSquared);
            var scanDist = wv.Radius + (stopLen * 3) + PADDING;
            var detected = false;

            for (int i = 0; i < 5; i++) {
                var lvd = ctr.LinearVelocityDirection;
                Vector3D scanPoint;
                if (ctr.LinearVelocity < 1.0) {

                    scanPoint = wv.Center + MAF.ranDir() * (wv.Radius * 5);
                } else {
                    scanPoint = wv.Center + lvd * scanDist;
                }
                var rd = MAF.ranDir();
                // keep scans on the far side of the sphere
                if (rd.Dot(lvd) < 0) {
                    rd = -rd;
                }
                // random distance from sphere center to scan
                var scanRadius = 0.1 + 2 * wv.Radius * MAF.random.NextDouble();

                scanPoint += rd * scanRadius;

                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (ctr.Camera.Scan(scanPoint, out entity, out thy)) {
                    if (
                        (thy != null && thy != mEntity) || 
                        (
                            thy == null && entity.EntityId != 0 && 
                            entity.Type != MyDetectedEntityType.CharacterHuman && 
                            entity.Type != MyDetectedEntityType.FloatingObject
                        )
                    ) {
                        // this is getting nasty
                        // mothership should only avoid objects owned by me if they have no velocity
                        // everything else should be aware of the mothership and gtfo of the way
                        if (!ctr.mManager.Mother || entity.Velocity.LengthSquared() < 0.1 ) {
                            ctr.logger.log($"Detected {entity.Name} {entity.Velocity.Length()}");
                            detected = true;
                            BoundingSphereD sphere;
                            if (onEscape) {
                                var dispFromThing = wv.Center - entity.HitPosition.Value;
                                var lengthFromThing = dispFromThing.Length();
                                var inverseDistFromThing = 1 / lengthFromThing;
                                mEscape.Add(dispFromThing * inverseDistFromThing);
                            }
                            if (thy == null) {
                                sphere = new BoundingSphereD(entity.HitPosition.Value, 10d);
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
            }
            if (detected) {
                mPreferredVelocityFactor -= 0.01;
            } else {
                mPreferredVelocityFactor += 0.01;
                mObstacle.Radius *= 0.99;
                if (mObstacle.Radius < 1) {
                    mObstacle.Radius = 0;
                }
            }
            mPreferredVelocityFactor = MathHelperD.Clamp(mPreferredVelocityFactor, 0.01, 1.0);
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
                        foreach (var e in mEscape) {
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
        
        protected void FlyTo(double maxVelo = 100.0) {

            ctr.Thrust.Damp = false;
            //var distSq = mDistToDest * mDistToDest;
            //var stopDistSq = mStop * mStop;
            //var syncVelo = ctr.ShipVelocities.LinearVelocity - BaseVelocity;
            //var syncVeloLen = syncVelo.Length();
            var accelerating = mPrefVelo > ctr.LinearVelocity;

            var curVelo = ctr.LocalLinearVelo;
            var localDir = MAF.world2dir(mDirToDest, ctr.MyMatrix);
            var veloVec = localDir * MathHelperD.Clamp(mPrefVelo * 0.9, 0.0, maxVelo);
            //veloVec += BaseVelocity;
            if (BaseVelocity.IsZero()) {
                if (accelerating) {
                    if (ctr.LinearVelocity > mPrefVelo) {
                        curVelo = veloVec;
                    }
                } else {
                    if (ctr.LinearVelocity < mPrefVelo) {
                        curVelo = veloVec;
                    }
                }
            } else {
                accelerating = false;
                var localBase = MAF.world2dir(BaseVelocityDirection, ctr.MyMatrix) * BaseVelocityLength;
                curVelo -= localBase;
            }

            

            var disp = (veloVec - curVelo);

            if (accelerating && disp.LengthSquared() < 2.0) {
                ctr.Thrust.Acceleration = 6.0 * disp;
            } else {
                ctr.Thrust.Acceleration = 6.0 * disp;
            }

        }
        // todo make FlyAt(direction) method
        // call flyat from here
        // call fly at from flyTo
        protected void collisionDetectTo() {
            var wv = ctr.Volume;
            var m = ctr.MyMatrix;
            var worldDir = collisionDetect();
            ctr.Thrust.Damp = false;
            if (worldDir.IsZero()) {
                worldDir = mDirToDest;
                ctr.logger.log("Following vector to destination.");
            } else {
                ctr.logger.log("Following avoidance vector.");
            }
            var localDir = MAF.world2dir(worldDir, m);

            //ctr.logger.log("dist ", dist);
            ctr.logger.log("Estimated arrival ", (mDistToDest / ctr.LinearVelocity) / 60.0, " minutes");


            var preferredVelocity = MathHelperD.Clamp(ctr.Thrust.PreferredVelocity(mMaxAccelLength, mDistToDest), 0.0, MAXVELO);

            ctr.logger.log("preferredVelocity ", preferredVelocity);
            var preferredVelocityVector = localDir * (preferredVelocity * mPreferredVelocityFactor);

            var accelReq = preferredVelocityVector - ctr.LocalLinearVelo;
            if (MAF.nearEqual(accelReq.LengthSquared(), 0, 0.0001)) {
                accelReq = Vector3D.Zero;
            }
            //var vrsq = veloReq.LengthSquared();


            if (mDistToDest < 1) {
                //ctr.Gyro.SetTargetDirection(ctr.Thrust.Acceleration = Vector3D.Zero);
            } else if (mDistToDest < 100) {

                ctr.Thrust.Acceleration = (mDistToDest * mDistToDest > 2.0 ? 6.0 : 1.0) * accelReq;
                //ctr.Gyro.SetTargetDirection(Vector3D.Zero);
            } else {
                ctr.Thrust.Acceleration = 6.0 * accelReq;
                //ctr.Gyro.SetTargetDirection(ctr.LinearVelocityDirection);
            }
            
        }
        public enum Details {
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
