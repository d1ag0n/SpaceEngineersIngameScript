using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
namespace IngameScript {
    
    public class DrillMission : MissionBase {
        readonly Vector3D missionTarget;
        readonly Vector3D missionStart;
        readonly Vector3D missionDirection;
        readonly ATCLientModule mATC;
        Action onUpdate;
        double deepestDepth;
        double lastDepth;
        double entranceDepth;

        readonly List<IMyShipDrill> mDrill = new List<IMyShipDrill>();
        
        public DrillMission(ShipControllerModule aController, Vector3D aPos) : base(aController, new BoundingSphereD(aPos, 0d)) {
            
            ctr.GetModule(out mATC);
            aPos = new Vector3D(943515.72, 1233397d, 885858.7);
            ATCLientModule atc;
            ctr.GetModule(out atc);
            if (atc != null) {
                atc.Connector.Enabled = false;
            }

            ctr.mManager.getByType(mDrill);
            
            if (ctr.Remote == null) {
                ctr.logger.persist("Remote Null?");
            }
            missionStart = ctr.Remote.CenterOfMass;
            missionTarget = aPos;
            missionDirection = Vector3D.Normalize(missionTarget - missionStart);
            missionStart += -missionDirection * 1000d;
            ctr.Thrust.Damp = false;
            onUpdate = enter;
        }
        public override void Update() => onUpdate();
        
        void enter() {
            ctr.logger.log("enter");
            var flResult = followLine(5.0);

            var maxAccel = ctr.Thrust.MaxAccel(ctr.LocalLinearVelo);
            
            var stop = ctr.Thrust.Stop(maxAccel);
            
            
            var wv = ctr.Volume;
            var scanPos = wv.Center + missionDirection * wv.Radius * 2.0;
            scanPos += MAF.ranDir() * wv.Radius;
            MyDetectedEntityInfo entity;
            ThyDetectedEntityInfo thy;
            ctr.Camera.Scan(scanPos, out entity, out thy);
            if (entity.Type == MyDetectedEntityType.Asteroid) {
                
                var ct = wv.Contains(entity.HitPosition.Value);
                var disp = wv.Center - entity.HitPosition.Value;
                var dist = disp.LengthSquared();
                if (dist < (wv.Radius * wv.Radius) + 400d) {
                    startDrill();
                    onUpdate = drill;
                }
            }
            //lastDepth = dlResult;
        }
        void drill() {
            ctr.logger.log("drill");
            ctr.logger.log($"lastDepth={lastDepth}");
            ctr.logger.log($"deepestDepth={deepestDepth}");
            ctr.Gyro.SetTargetDirection(missionDirection);

            var dist = followLine(lastDepth - 1d < deepestDepth ? 1.0 : 0.04);

            if (ctr.cargoLevel() > 0.15f) {
                stopDrill();
                onUpdate = extract;
            } else {
                
            }
            lastDepth = dist;
        }
        void extract() {
            ctr.logger.log("extract");
            if (followLine(5.0, true) < entranceDepth + 1d) {
                
                ctr.ExtendMission(new DockMission(ctr, mATC));
                onUpdate = undock;
            }
        }
        void undock() {
            mATC.Connector.Enabled = false;
            onUpdate = enter;
        }

        double followLine(double aSpeed = 0.04, bool reverse = false) {
            
            var r = ctr.Remote;
            var com = r.CenterOfMass;
            var disp = com - missionStart;
            var dir = disp;
            var dist = dir.Normalize() + 1.0;

            Vector3D pos = missionStart + missionDirection * dist;
            if (reverse) {
                pos = missionStart;
            }
            
            var m = ctr.MyMatrix;
            pos = MAF.world2pos(pos, m);
            dir = pos;
            var offCourse = dir.Normalize();
            ctr.logger.log($"offCourse={offCourse:f0}");
            if (!reverse) {
                if (offCourse < 3.0 && deepestDepth < dist) {
                    deepestDepth = dist;
                } else {
                    aSpeed = 1.0;
                }
            }
            ctr.logger.log($"deepestDepth={deepestDepth:f0}");
            ctr.logger.log($"entranceDepth={entranceDepth:f0}");
            dir *= aSpeed;
            ctr.Thrust.Acceleration = dir - ctr.LocalLinearVelo;
            ctr.logger.log($"followLine={dist:f0}");
            return dist;
        }



        void startDrill() {
            foreach (var d in mDrill) {
                d.Enabled = true;
            }
            ctr.Gyro.Roll = 0.2f;
        }

        void stopDrill() {
            foreach (var d in mDrill) {
                d.Enabled = false;
            }
            ctr.Gyro.Roll = 0f;
        }
        
    }
}
