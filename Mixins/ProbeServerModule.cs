using System;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;
using VRage;

namespace IngameScript {
    public class ProbeServerModule : Module<IMyTerminalBlock> {
        readonly IMyBroadcastListener mListener;
        readonly HashSet<long> mProbeSet = new HashSet<long>();
        readonly List<long> mProbes = new List<long>();
        public bool KnownProbe(long entityId) => mProbeSet.Contains(entityId);
        public ProbeServerModule() {

            mListener = ModuleManager.Program.IGC.RegisterBroadcastListener("Register");
            onUpdate = UpdateAction;
        }
        public override bool Accept(IMyTerminalBlock aBlock) => false;
        DateTime lastUpdate = DateTime.MinValue;
        void UpdateAction() {
            while (mListener.HasPendingMessage) {
                var m = mListener.AcceptMessage();
                if (mProbeSet.Add(m.Source)) {
                    mProbes.Add(m.Source);
                    logger.persist("Adding probe to registry.");
                }
            }
            var time = DateTime.Now;
            if ((time - lastUpdate).TotalSeconds > 1.0) {
                var v = controller.ShipVelocities.LinearVelocity;
                var s = 0.0;
                if (!v.IsZero()) {
                    s = v.Normalize();
                }
                var t = ProbeFollow(ModuleManager.WorldVolume, v, s);
                foreach (var p in mProbes) {
                    logger.log("Sending to probe.");
                    ModuleManager.Program.IGC.SendUnicastMessage(p, "probe", t);
                }
            }
        }
        public static MyTuple<BoundingSphereD, Vector3D, double> ProbeFollow(BoundingSphereD position, Vector3D dir, double speed) => MyTuple.Create(position, dir, speed);
        public static MyTuple<BoundingSphereD, Vector3D, double> ProbeFollow(object data) => (MyTuple<BoundingSphereD, Vector3D, double>)data;
    }
}
