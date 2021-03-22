using System;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ProbeServerModule : Module<IMyTerminalBlock> {
        readonly IMyBroadcastListener mListener;
        readonly HashSet<long> mProbeSet = new HashSet<long>();
        readonly List<long> mProbes = new List<long>();
        readonly ShipControllerModule ctr;
        public ProbeServerModule() {
            GetModule(out ctr);
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
                }
            }
            var time = DateTime.Now;
            if ((time - lastUpdate).TotalSeconds > 1.0) {
                var m = ctr.Remote.WorldMatrix;
                var wv = ModuleManager.WorldVolume;
                double d = 0.0;
                foreach (var p in mProbes) {
                    ModuleManager.Program.IGC.SendUnicastMessage(p, "probe", wv.Center + m.Backward * (wv.Radius + d));
                    d += 20.0;
                }
            }
        }
    }
}

