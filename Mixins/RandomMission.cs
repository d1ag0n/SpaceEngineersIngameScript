using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    /// <summary>
    /// Clustering Orbital Collision Mitigation
    /// </summary>
    class RandomMission : MissionBase {
        static int next = 0;
        
        public RandomMission(ShipControllerModule aController) : base(aController) {
            Vector3D dir = Vector3D.Zero;
            switch (next) {
                case 0: dir = ctr.Remote.WorldMatrix.Forward; break;
                case 1: dir = ctr.Remote.WorldMatrix.Backward; break;
                case 2: dir = ctr.Remote.WorldMatrix.Left; break;
                case 3: dir = ctr.Remote.WorldMatrix.Right; break;
                case 4: dir = ctr.Remote.WorldMatrix.Up; break;
                case 5: dir = ctr.Remote.WorldMatrix.Down; break;
            }
            next++;
            if (next == 6) {
                next = 0;
            }
            dir = MAF.ranDir();
            mDestination = ctr.Remote.CenterOfMass + dir * 1100.0;
            ctr.logger.persist(ctr.logger.gps("RandomMission", mDestination));
        }
        
        public override void Update() {
            FlyTo();
            return;
            if (!ctr.Damp) {
                var com = ctr.Remote.CenterOfMass;
                var disp = mDestination - com;
                if (disp.LengthSquared() > 1.0 || ctr.LinearVelocity > 0.1) {
                    ctr.Damp = false;
                    var dir = disp;
                    var dist = dir.Normalize();
                    var prefVelo = ctr.Thrust.PreferredVelocity(-Vector3D.Normalize(ctr.LocalLinearVelo), dist);

                    if (ctr.LinearVelocity == 0) {
                        prefVelo = 1.0;
                    } else {
                        //prefVelo = ctr.Thrust.PreferredVelocity(-Vector3D.Normalize(ctr.LocalLinearVelo), dist);
                        if (prefVelo > dist) {
                            prefVelo = dist;
                        }
                    }
                    var accelerating = prefVelo > ctr.LinearVelocity;


                    ctr.logger.log("accelerating ", accelerating);
                    ctr.logger.log("Preferred Velocity ", prefVelo);
                    ctr.logger.log("Distance ", dist);
                    ctr.logger.log("Stop ", ctr.Thrust.StopDistance);
                    ctr.logger.log("Full Stop ", ctr.Thrust.FullStop);
                    var curVelo = ctr.LocalLinearVelo;
                    var localDir = MAF.world2dir(dir, ModuleManager.WorldMatrix);
                    var veloVec = localDir * prefVelo;
                    prefVelo = MathHelperD.Clamp(prefVelo, 0.0, 99.8);
                    if (!accelerating) {
                        if (ctr.LinearVelocity < prefVelo) {
                            curVelo = veloVec;
                        }
                    } else {
                        if (ctr.LinearVelocity > prefVelo) {
                            curVelo = veloVec;
                        }
                    }
                    //if (dist < ctr.Thrust.StopDistance) {
                      //  ctr.Thrust.Emergency = true;
                        //ctr.Damp = true;
                    //} else {
                        //ctr.Damp = false;
                        ctr.Thrust.Acceleration = 6.0 * (veloVec - curVelo);
                    //}
                    
                    


                } else {
                    Complete = true;
                    ctr.Damp = true;
                    ctr.Thrust.Emergency = false;
                }
            }
        }
    }
}

