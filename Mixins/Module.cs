using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {

    class Module<T> : IAccept {
        readonly HashSet<long> mRegistry = new HashSet<long>();
        public virtual bool Accept(IMyTerminalBlock b) {
            var result = false;
            if (b is T) {
                if (mRegistry.Add(b.EntityId)) {
                    result = true;
                }
            }
            return result;
        }
        public virtual bool Remove(IMyTerminalBlock b) => mRegistry.Remove(b.EntityId);
    }
}