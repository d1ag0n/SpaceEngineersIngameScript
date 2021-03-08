using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    static class ModuleManager {
        static readonly Dictionary<int, List<IAccept>> modules = new Dictionary<int, List<IAccept>>();
        public static void Add<T>(Module<T> m) {
            Logger g;
            List<IAccept> list;

            var t = m.GetType();
            var hash = t.GetHashCode();

            if (GetModule(out g)) {
                g.log(t);
            }
            
            if (!modules.TryGetValue(hash, out list)) {
                list = new List<IAccept>();
                modules.Add(hash, list);
            }
            list.Add(m);
        }
        public static bool GetModule<S>(out S aComponent) {
            var hash = typeof(S).GetHashCode();
            List<IAccept> list;
            if (modules.TryGetValue(hash, out list)) {
                foreach (var m in list) {
                    if (m is S) {
                        aComponent = (S)m;
                        return true;
                    } else {
                        throw new Exception($"m={m.GetType()}, S={typeof(S)}");
                    }
                }
            }
            aComponent = default(S);
            return false;
        }
        public static bool GetModules<S>(List<S> aComponentList) {
            var hash = typeof(S).GetHashCode();
            List<IAccept> list;
            if (modules.TryGetValue(hash, out list)) {
                foreach (var m in list) {
                    aComponentList.Add((S)m);
                }
                return true;
            }
            return false;
        }
    }
    class Module<T> : IAccept {
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