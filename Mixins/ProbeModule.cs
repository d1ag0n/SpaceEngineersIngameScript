using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ProbeModule : Module<IMyTerminalBlock> {
        readonly ShipControllerModule ctr;
        readonly GyroModule gyro;
        readonly ThrustModule thrust;
        readonly HashSet<long> probes;
        readonly List<IMySolarPanel> mPanels = new List<IMySolarPanel>();
        readonly List<IMyBatteryBlock> mBatteries = new List<IMyBatteryBlock>();

        public ProbeModule() {
            ctr = ModuleManager.controller;
            ModuleManager.IGCSubscribe("probe", ProbeMessage);
            GetModule(out gyro);
            GetModule(out thrust);
            ctr.Damp = true;
            onUpdate = RegisterAction;
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = false;
            if (ModuleManager.Probe) {
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
        Vector3D Target;

        void ProbeMessage(MyIGCMessage m) {
            Target = (Vector3D)m.Data;
            onUpdate = UpdateAction;
        }
        void RegisterAction() {
            ModuleManager.Program.IGC.SendBroadcastMessage("Register", 1);
        }
        // Vector3D absoluteNorthVecPlanetWorlds = new Vector3D(0, -1, 0); //this was determined via Keen's code
        // Vector3D absoluteNorthVecNotPlanetWorlds = new Vector3D(0.342063708833718, -0.704407897782847, -0.621934025954579); //this was determined via Keen's code
        void UpdateAction() {
            
            if (Target == Vector3D.Zero) {
                ctr.Damp = true;
            } else {
                var wv = ctr.Grid.WorldVolume;
                var disp = Target - wv.Center;
                if (disp.LengthSquared() > 4) {
                    ctr.Damp = false;
                    var dir = disp;
                    var dist = dir.Normalize();
                    //var prefVelo = ctr.Thrust.PreferredVelocity(dist, 75.0);
                    var prefVelo = ctr.Thrust.PreferredVelocity(-dir, dist);
                    
                    
                    ctr.logger.log("Preferred Velocity ", prefVelo);
                    ctr.logger.log("Distance ", dist);
                    ctr.logger.log("Stop ", ctr.Thrust.StopDistance);
                    ctr.logger.log("Full Stop ", ctr.Thrust.FullStop);
                    var localDir = MAF.world2dir(dir, ModuleManager.WorldMatrix);
                    var veloVec = localDir * prefVelo;
                    ctr.Thrust.Acceleration = veloVec - ctr.LocalLinearVelo;
                } else {
                    ctr.Damp = true;
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
