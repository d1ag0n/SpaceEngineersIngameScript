using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public class Module<T> : IAccept {
        readonly HashSet<long> mRegistry = new HashSet<long>();

        public Module() {
            ModuleManager.Add(this);
        }

        public bool GetModule<S>(out S aComponent) => ModuleManager.GetModule(out aComponent);
        public bool GetModules<S>(List<S> aComponentList) => ModuleManager.GetModules(aComponentList);
        
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