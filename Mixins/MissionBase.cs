using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    public abstract class MissionBase {
        protected Vector3D mDestination;
        public bool Complete { get; protected set; }
        protected readonly ShipControllerModule ctr;
        public MissionBase(ShipControllerModule aController) {
            ctr = aController;
        }
        public abstract void Update();
        protected void FlyTo(double maxVelo = 100.0) {
            var shipCenter = ctr.Grid.WorldVolume.Center;
            var disp = mDestination - shipCenter;
            if (disp.LengthSquared() > 1.0 || ctr.LinearVelocity > 0.1) {
                ctr.Damp = false;
                var dir = disp;
                var dist = dir.Normalize();
                var prefVelo = ctr.Thrust.PreferredVelocity(-Vector3D.Normalize(ctr.LocalLinearVelo), dist);
                if (ctr.LinearVelocity == 0) {
                    prefVelo = 1.0;
                } else {
                    if (prefVelo > dist) {
                        prefVelo = dist;
                    }
                }
                var accelerating = prefVelo > ctr.LinearVelocity;
                var curVelo = ctr.LocalLinearVelo;
                var localDir = MAF.world2dir(dir, ModuleManager.WorldMatrix);
                var veloVec = localDir * prefVelo;
                prefVelo = MathHelperD.Clamp(prefVelo, 0.0, maxVelo);
                if (!accelerating) {
                    if (ctr.LinearVelocity < prefVelo) {
                        curVelo = veloVec;
                    }
                } else {
                    if (ctr.LinearVelocity > prefVelo) {
                        curVelo = veloVec;
                    }
                }
                if (false && dist < ctr.Thrust.StopDistance) {
                    ctr.Thrust.Emergency = true;
                    ctr.Damp = true;
                } else {
                    ctr.Damp = false;
                    disp = (veloVec - curVelo);
                    if (accelerating && disp.LengthSquared() < 2.0) {
                        ctr.Thrust.Acceleration = disp;
                    } else {
                        ctr.Thrust.Acceleration = 6.0 * disp;
                    }
                }
            } else {
                ctr.Damp = true;
                ctr.Thrust.Emergency = false;
            }
        }
    }
}
