using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ProbeModule : Module<IMyTerminalBlock> {

        
        readonly List<IMySolarPanel> mPanels = new List<IMySolarPanel>();
        readonly List<IMyBatteryBlock> mBatteries = new List<IMyBatteryBlock>();
        readonly List<IMyRadioAntenna> mAntennas = new List<IMyRadioAntenna>();

        bool rolling = false;
        const float maxEver = 0.04f;
        float maxInRoll = 0;
        int rollCount = 0;
        bool charge = false;
        IMySolarPanel maxPanel;
        readonly GyroModule mGyro;
        readonly ThrustModule mThrust;
        readonly ATClientModule mATC;

        public ProbeModule(ModuleManager aManager):base(aManager) {
            aManager.GetModule(out mGyro);
            aManager.GetModule(out mThrust);
            aManager.GetModule(out mATC);
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
        





        // Vector3D absoluteNorthVecPlanetWorlds = new Vector3D(0, -1, 0); //this was determined via Keen's code
        // Vector3D absoluteNorthVecNotPlanetWorlds = new Vector3D(0.342063708833718, -0.704407897782847, -0.621934025954579); //this was determined via Keen's code
        const double PADDING = 100.0;
        void UpdateAction() {
            var ms = mATC.Mother;
            if (mManager.Runtime - ms.LastUpdate > 1) {
                mThrust.Damp = true;
            } else {
                mThrust.Damp = false;
                var wv = mController.Grid.WorldVolume; 
                var minDist = ms.Sphere.Radius + wv.Radius + PADDING;
                var maxDist = minDist + PADDING;
                var dispToMother = ms.Sphere.Center - wv.Center;
                var dirToMother = dispToMother;
                var distToMother = dirToMother.Normalize();

                double dist = 0;
                Vector3D baseVec = ms.VeloDir * ms.Speed;


                var syncVec = Vector3D.Zero;
                if (distToMother > maxDist) {
                    syncVec = dirToMother;
                    dist = distToMother - maxDist;
                } else if (distToMother < minDist) {
                    if (ms.Speed > 1) {
                        syncVec = Vector3D.Normalize(wv.Center - MAF.orthoProject(ms.Sphere.Center + ms.VeloDir, wv.Center, dirToMother));
                    } else {
                        dirToMother.CalculatePerpendicularVector(out syncVec);
                    }
                    dist = minDist - distToMother;
                }
                var llv = mController.LocalLinearVelo;
                
                if (dist > 0) {
                    if (mController.LinearVelocity > 0.0) {
                        var maxAccelLength = mThrust.MaxAccel(syncVec).Length();
                        var prefVelo = MathHelperD.Clamp(mThrust.PreferredVelocity(maxAccelLength, dist), 0.0, 25.0);
                        
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
                    mThrust.Acceleration = accelVec;
                } else {
                    mThrust.Acceleration = 6.0 * accelVec;
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
                if (mController.Remote.WorldMatrix.Forward.Dot(dir) < 0) {
                    dir = Vector3D.Up;
                    roll = 0.1f;
                }
                mGyro.SetTargetDirection(dir);
                if (rolling) {
                    mGyro.Yaw = 0f;
                    mGyro.Roll = roll;
                } else {
                    mGyro.Roll = 0f;
                    mGyro.Yaw = 0.05f;
                }
            } else {
                mGyro.Roll = mGyro.Yaw = 0f;
                // can use arbitrary direction
            }
        }
    }
}
