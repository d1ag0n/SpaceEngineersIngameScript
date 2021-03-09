using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public class Module<T> : IAccept {

        public static IAccept Factory() { throw new Exception("Please implement this method on the inheritiing class."); }

        readonly HashSet<long> mRegistry = new HashSet<long>();

        private List<T> _blocks;
        public List<T> Blocks {
            get {
                if (_blocks == null) {
                    _blocks = new List<T>();
                }
                return _blocks;
            }
        }
        public Module() {
            ModuleManager.Add(this);
        }
        public bool GetModule<S>(out S aComponent) => ModuleManager.GetModule(out aComponent);
        public bool GetModules<S>(List<S> aComponentList) => ModuleManager.GetModules(aComponentList);
        public virtual bool Accept(IMyTerminalBlock b) {
            var result = false;
            if (b is T) {
                if (mRegistry.Add(b.EntityId)) {
                    Blocks.Add((T)b);
                    result = true;
                }
            }
            return result;
        }
        public virtual bool Remove(IMyTerminalBlock b) {
            if (b is T) {
                Blocks.Remove((T)b);
                mRegistry.Remove(b.EntityId);
                return true;
            }
            return false;
        }
    }
}