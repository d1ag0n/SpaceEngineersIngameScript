using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ProbeModule : Module<IMyTerminalBlock> {
        readonly ShipControllerModule ctr;
        
        readonly List<IMySolarPanel> mPanels = new List<IMySolarPanel>();
        readonly List<IMyBatteryBlock> mBatteries = new List<IMyBatteryBlock>();
        readonly List<IMyRadioAntenna> mAntennas = new List<IMyRadioAntenna>();
       
        
        public ProbeModule(ModuleManager aManager):base(aManager) {
            ctr = aManager.controller;
            onUpdate = UpdateAction;
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = false;

            if (mManager.Probe) {
                if (aBlock is IMyRadioAntenna) {
                    var ant = aBlock as IMyRadioAntenna;
                    ant.Radius = 1f;
                    ant.Enabled = true;
                    mAntennas.Add(ant);
                }
                if (aBlock is IMyBatteryBlock) {
                    mBatteries.Add(aBlock as IMyBatteryBlock);
                    return true;
                }
                if (aBlock is IMySolarPanel) {
                    mPanels.Add(maxPanel = aBlock as IMySolarPanel);
                    return true;
                }
            }
            return result;
        }
        
        bool rolling = false;
        const float maxEver = 0.04f;
        float maxInRoll = 0;
        int rollCount = 0;
        bool charge = false;
        IMySolarPanel maxPanel;




        // Vector3D absoluteNorthVecPlanetWorlds = new Vector3D(0, -1, 0); //this was determined via Keen's code
        // Vector3D absoluteNorthVecNotPlanetWorlds = new Vector3D(0.342063708833718, -0.704407897782847, -0.621934025954579); //this was determined via Keen's code
        const double PADDING = 100.0;
        void UpdateAction() {
            if (mManager.Runtime - ctr.MotherLastUpdate > 1) {
                controller.Thrust.Damp = true;
            } else {
                ctr.Thrust.Damp = false;
                var wv = ctr.Grid.WorldVolume; 
                var minDist = ctr.MotherSphere.Radius + wv.Radius + PADDING;
                var maxDist = minDist + PADDING;
                var dispToMother = ctr.MotherSphere.Center - wv.Center;
                var dirToMother = dispToMother;
                var distToMother = dirToMother.Normalize();

                double dist = 0;
                Vector3D baseVec = ctr.MotherVeloDir * ctr.MotherSpeed;


                var syncVec = Vector3D.Zero;
                if (distToMother > maxDist) {
                    syncVec = dirToMother;
                    dist = distToMother - maxDist;
                } else if (distToMother < minDist) {
                    if (ctr.MotherSpeed > 1) {
                        syncVec = Vector3D.Normalize(wv.Center - MAF.orthoProject(ctr.MotherSphere.Center + ctr.MotherVeloDir, wv.Center, dirToMother));
                    } else {
                        dirToMother.CalculatePerpendicularVector(out syncVec);
                    }
                    dist = minDist - distToMother;
                }
                var llv = ctr.LocalLinearVelo;
                
                if (dist > 0) {
                    if (ctr.LinearVelocity > 0.0) {
                        var maxAccelLength = ctr.Thrust.MaxAccel(syncVec).Length();
                        var prefVelo = MathHelperD.Clamp(ctr.Thrust.PreferredVelocity(maxAccelLength, dist), 0.0, 25.0);
                        
                        syncVec *= prefVelo;
                        baseVec += syncVec;
                    }
                }
                
                var baseVelo = baseVec.Normalize();
                var localDir = MAF.world2dir(baseVec, MyMatrix);
                var veloVec = localDir * baseVelo;
                var accelVec = veloVec - llv;
                var accelVecLenSq = accelVec.LengthSquared();
                if (accelVecLenSq < 2.0) {
                    ctr.Thrust.Acceleration = accelVec;
                } else {
                    ctr.Thrust.Acceleration = 6.0 * accelVec;
                }
                
            }

            var stored = 0f;
            var max = 0f;

            foreach(var b in mBatteries) {
                stored += b.CurrentStoredPower;
                max += b.MaxStoredPower;
            }

            float percent = stored / max;

            if (percent < 0.2f) {
                charge = true;
            } else if (percent > 0.99f) {
                charge = false;
            }

            charge = true;

            if (charge) {
                percent = maxPanel.MaxOutput / maxEver;
                if (rolling) {
                    rollCount++;
                    if (percent > maxInRoll) {
                        rollCount--;
                        maxInRoll = percent;
                    } else if (rollCount > 18) {
                        rolling = false;
                    }
                } else {
                    if (percent < 0.9f) {
                        rollCount = 0;
                        rolling = true;
                        maxInRoll = percent;
                    }
                }
                var roll = -0.1f;
                var dir = Vector3D.Down;
                if (ctr.Remote.WorldMatrix.Forward.Dot(dir) < 0) {
                    dir = Vector3D.Up;
                    roll = 0.1f;
                }
                ctr.Gyro.SetTargetDirection(dir);
                if (rolling) {
                    ctr.Gyro.Yaw = 0f;
                    ctr.Gyro.Roll = roll;
                } else {
                    ctr.Gyro.Roll = 0f;
                    ctr.Gyro.Yaw = 0.05f;
                }
            } else {
                ctr.Gyro.Roll = ctr.Gyro.Yaw = 0f;
                // can use arbitrary direction
            }
        }
    }
}
