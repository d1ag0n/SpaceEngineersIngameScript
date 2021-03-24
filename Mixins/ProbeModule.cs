using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ProbeModule : Module<IMyTerminalBlock> {
        readonly ShipControllerModule ctr;
        readonly HashSet<long> probes;
        readonly List<IMySolarPanel> mPanels = new List<IMySolarPanel>();
        readonly List<IMyBatteryBlock> mBatteries = new List<IMyBatteryBlock>();
        readonly List<IMyRadioAntenna> mAntennas = new List<IMyRadioAntenna>();
        readonly GyroModule gyro;
        int name;
        public ProbeModule() {
            ctr = ModuleManager.controller;
            ModuleManager.IGCSubscribe("ProbeFollow", ProbeFollow);
            GetModule(out gyro);
            
            ctr.Damp = true;
            onUpdate = RegisterAction;
            
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = false;

            if (ModuleManager.Probe) {
                if (aBlock is IMyRadioAntenna) {
                    mAntennas.Add(aBlock as IMyRadioAntenna);
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
        public void Register(MyIGCMessage m) => probes.Add(m.Source);
        bool rolling = false;
        const float maxEver = 0.04f;
        float maxInRoll = 0;
        int rollCount = 0;
        bool charge = false;
        IMySolarPanel maxPanel;
        BoundingSphereD MotherSphere;
        Vector3D MotherVeloDir;
        double MotherSpeed;
        DateTime lastUpdate;

        void ProbeFollow(MyIGCMessage m) {
            var pf = ProbeServerModule.ProbeFollow(m.Data);
            MotherSphere = pf.Item1;
            MotherVeloDir = pf.Item2;
            MotherSpeed = pf.Item3;
            onUpdate = UpdateAction;
            lastUpdate = DateTime.Now;
            if (pf.Item4 != name) {
                name = pf.Item4;
                var one = true;
                foreach (var a in mAntennas) {
                    a.CustomName = $"Probe {name:D2}";
                    a.EnableBroadcasting =
                    a.Enabled = one;
                    one = false;
                }
            }
        }
        void RegisterAction() {
            ModuleManager.Program.IGC.SendBroadcastMessage("Register", 1);
        }
        // Vector3D absoluteNorthVecPlanetWorlds = new Vector3D(0, -1, 0); //this was determined via Keen's code
        // Vector3D absoluteNorthVecNotPlanetWorlds = new Vector3D(0.342063708833718, -0.704407897782847, -0.621934025954579); //this was determined via Keen's code
        const double PADDING = 100.0;
        void UpdateAction() {
            if ((DateTime.Now - lastUpdate).TotalSeconds > 1) {
                RegisterAction();
                controller.Damp = true;
            } else {
                ctr.Damp = false;
                var wv = ctr.Grid.WorldVolume; 
                var minDist = MotherSphere.Radius + wv.Radius + PADDING;
                var maxDist = minDist + PADDING;
                var dispToMother = MotherSphere.Center - wv.Center;
                var dirToMother = dispToMother;
                var distToMother = dirToMother.Normalize();

                double dist = 0;
                Vector3D baseVec = MotherVeloDir * MotherSpeed;

                logger.log("MotherSpeed ", MotherSpeed);

                var syncVec = Vector3D.Zero;
                if (distToMother > maxDist) {
                    syncVec = dirToMother;
                    dist = distToMother - maxDist;
                } else if (distToMother < minDist) {
                    dirToMother.CalculatePerpendicularVector(out syncVec);
                    dist = minDist - distToMother;
                }
                logger.log("Mother dist ", minDist, " - ", dist, " - ", maxDist);
                var llv = ctr.LocalLinearVelo;
                
                if (dist > 0) {
                    if (ctr.LinearVelocity > 0.0) {
                        var maxAccelLength = ctr.Thrust.MaxAccel(syncVec).Length();
                        var prefVelo = MathHelperD.Clamp(ctr.Thrust.PreferredVelocity(maxAccelLength, dist), 0.0, 25.0);
                        logger.log("prefVelo ", prefVelo);
                        
                        syncVec *= prefVelo;
                        logger.log("syncVec Length ", syncVec.Length());
                        logger.log("baseVec Length ", baseVec.Length());
                        baseVec += syncVec;
                        logger.log("baseVec + syncVec Length ", baseVec.Length());
                    }
                }
                
                var baseVelo = baseVec.Normalize();
                logger.log("baseVelo ", baseVelo);
                //ctr.logger.log("Stop ", ctr.Thrust.StopDistance);
                //ctr.logger.log("Full Stop ", ctr.Thrust.FullStop);
                var localDir = MAF.world2dir(baseVec, ModuleManager.WorldMatrix);
                var veloVec = localDir * baseVelo;
                var accelVec = veloVec - llv;
                var accelVecLenSq = accelVec.LengthSquared();
                logger.log("accelVecLenSq ", accelVecLenSq);
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

            ctr.logger.log("battery % ", percent);
            //charge = true;

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
