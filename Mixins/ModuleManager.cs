using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public static class ModuleManager {
        static readonly List<IAccept> allModules = new List<IAccept>();
        static readonly Dictionary<int, List<IAccept>> modules = new Dictionary<int, List<IAccept>>();
        public static bool Accept(IMyTerminalBlock b) {
            foreach (var m in allModules) {
                m.Accept(b);
            }
            return false;
        }
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
            allModules.Add(m);
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

}