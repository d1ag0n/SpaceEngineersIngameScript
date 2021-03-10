using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public class Module<T> : ModuleBase, IAccept {
        readonly HashSet<long> mRegistry = new HashSet<long>();

        public readonly List<T> Blocks = new List<T>();

        public Module() {
            ModuleManager.Add(this);
        }
        
        public bool GetModule<S>(out S aComponent) => ModuleManager.GetModule(out aComponent);
        public bool GetModules<S>(List<S> aComponentList) => ModuleManager.GetModules(aComponentList);
        public virtual bool Accept(IMyTerminalBlock b) {
            if (b is T) {
                if (mRegistry.Add(b.EntityId)) {
                    Blocks.Add((T)b);
                    return true;
                }
            }
            return false;
        }
        public virtual bool Remove(IMyTerminalBlock b) {
            if (b is T) {
                Blocks.Remove((T)b);
                mRegistry.Remove(b.EntityId);
                return true;
            }
            return false;
        }

        public virtual void Update() { }
    }
}