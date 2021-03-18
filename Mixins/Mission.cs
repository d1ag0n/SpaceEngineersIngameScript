using Sandbox.Definitions;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Mission
    {
        public Mission Previous;
        //public Connector Connector;

        public Details Detail;
        public double Altitude;
        public double Distance;

        public int Step;
        public int SubStep;
        
        /// <summary>
        /// where the mission started
        /// </summary>
        public Vector3D Start;

        private readonly ThyDetectedEntityInfo Target;
        private ThyDetectedEntityInfo Orbit;

        private readonly ShipControllerModule ctr;
        private readonly Stack<Vector3D> mWaypoints = new Stack<Vector3D>();
        /// <summary>
        /// arbitrary direction value
        /// </summary>
        public Vector3D PendingDirection;
        /// <summary>
        /// arbitrary 
        /// </summary>
        public Vector3D PendingPosition;

        

        public Mission(ShipControllerModule aController, ThyDetectedEntityInfo aTarget) {
            ctr = aController;
            Target = aTarget;
            
            var disp = Target.Position - ctr.Grid.WorldVolume.Center;
            var dir = disp;
            var dist = dir.Normalize();
            dist -= ctr.Grid.WorldVolume.Radius;
            dist -= Target.WorldVolume.Radius;
            dist -= 100;
            
            var t = ctr.Grid.WorldVolume.Center + (dir * dist);
            aController.logger.persist(aController.logger.gps("MISSION", t));
            mWaypoints.Push(t);
        }

        private int scansPerTick = 6;

        VectorHandler currentJob;
        Vector3D[] arCorners = new Vector3D[8];
        public bool Complete { get; private set; }
        bool veloOkay = false;
        public void Update() {
            var target = mWaypoints.Peek();
            var wv = ctr.Grid.WorldVolume;
            var disp = target - wv.Center;
            var dir = disp;
            var dist = dir.Normalize();
            var velocityVec = ctr.ShipVelocities.LinearVelocity;
            var velocity = ctr.LinearVelocity;
            var preferredVelocity = 99.99;
            var veloReq = (dir * preferredVelocity) - velocityVec;
            
            ctr.logger.log("DIST ", dist);
            if (dist < 100 || dist < ctr.Thrust.StopDistance) {
                Complete = ctr.Damp = true;
                ctr.Gyro.SetTargetDirection(Vector3D.Zero);
            } else {
                var localVelo = MAF.world2dir(veloReq, ctr.Remote.WorldMatrix);
                var lvsq = localVelo.LengthSquared();
                if (ctr.Damp) {
                    scansPerTick = 6;
                } else if (!ctr.Damp) {
                    if (MAF.nearEqual(lvsq, 0)) {
                        ctr.Thrust.Acceleration = Vector3D.Zero;
                        ctr.Gyro.SetTargetDirection(Vector3D.Zero);
                    } else {
                        veloOkay = false;
                        ctr.Thrust.Acceleration = Vector3D.Normalize(localVelo);
                        ctr.Gyro.SetTargetPosition(target);
                    }
                }
            }
            var lvd = ctr.LinearVelocityDirection;
            if (scansPerTick < 100 && ModuleManager.Lag < 1) {
                scansPerTick++;
            } else if (scansPerTick > 6 && ModuleManager.Lag > 1.5) {
                scansPerTick--;
            }
            ctr.logger.log($"Scans Per Tick {scansPerTick}");
            for (int i = 0; i < scansPerTick; i++) {
                // use a random direction if we have little velo
                if (velocity < 0.1) {
                    velocity = 10;
                    lvd = MAF.ranDir();
                }
                // create a point for scan sphere based on our velocity
                var scanPoint = ctr.Grid.WorldVolume.Center;
                //ctr.logger.log("Scan base point ", Vector3D.Distance(ctr.Grid.WorldVolume.Center, scanPoint), "m away");
                
                //ctr.logger.log("Scan point + velo ", Vector3D.Distance(ctr.Grid.WorldVolume.Center, scanPoint), "m away");
                //ctr.logger.log("Direction length ", lvd.Length());
                scanPoint += (lvd * (ctr.Thrust.StopDistance + 500));
                //ctr.logger.log("Scan point + padding ", Vector3D.Distance(ctr.Grid.WorldVolume.Center, scanPoint), "m away");
                // random dir around point to scan
                var rd = MAF.ranDir();

                // keep scans on the far side of the sphere
                if (rd.Dot(lvd) < 0) {
                    rd = -rd;
                }

                // random distance from sphere center to scan
                var scandist = ctr.Grid.WorldVolume.Radius * MAF.random.NextDouble();
                
                scanPoint += rd * scandist;
                    
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (ctr.Camera.Scan(scanPoint, out entity, out thy)) {
                    if (entity.Type != MyDetectedEntityType.None && entity.EntityId != Target.EntityId) {
                        //ctr.logger.persist($"OH MY GOD A {entity.Type} we're gonna CRASH!");
                    }
                }
            }

            return;

            var obb = MAF.obb(ctr.Grid);

            obb.GetCorners(arCorners, 0);
            MyDetectedEntityInfo e;
            for(int i = 0; i < 8; i++) {
                //if (ctr.Camera.Scan(arCorners[i], out e)) {}
            }


            currentJob();
        }
        void calculateWaypoint(BoundingSphereD sphere) {
            var disp = sphere.Center - ctr.Grid.WorldVolume.Center;
            
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
