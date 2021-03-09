using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public static class ModuleManager {
        static readonly HashSet<long> mRegistry = new HashSet<long>();
        static readonly List<IMyTerminalBlock> mBlocks = new List<IMyTerminalBlock>();
        static readonly List<IAccept> mModules = new List<IAccept>();
        static readonly Dictionary<int, List<IAccept>> mModuleList = new Dictionary<int, List<IAccept>>();
        public static MyGridProgram Program { get; private set; }
        public static void Initialize(MyGridProgram aProgram = null) {
            if (Program == null) {
                if (aProgram == null) { 
                    throw new ArgumentException("Program cannot be null.");
                }
                Program = aProgram; 
            }
            Program.GridTerminalSystem.GetBlocksOfType(mBlocks, block => mRegistry.Add(block.EntityId));
            foreach (var module in mModules) {
                foreach (var block in mBlocks) {
                    module.Accept(block);
                }
            }
        }
        /*public static bool Accept(IMyTerminalBlock b) {
            foreach (var m in allModules) {
                m.Accept(b);
            }
            return false;
        }*/
        public static void Add<T>(Module<T> aModule) {
            Logger g;
            List<IAccept> list;

            var type = aModule.GetType();
            var hash = type.GetHashCode();

            if (!mModuleList.TryGetValue(hash, out list)) {
                list = new List<IAccept>();
                mModuleList.Add(hash, list);
            }
            list.Add(aModule);
            mModules.Add(aModule);
            foreach(var block in mBlocks) {
                aModule.Accept(block);
            }
        }
        public static bool GetModule<S>(out S aComponent) {
            var hash = typeof(S).GetHashCode();
            List<IAccept> list;
            if (mModuleList.TryGetValue(hash, out list)) {
                foreach (var m in list) {
                    if (m is S) {
                        aComponent = (S)m;
                        return true;
                    } else {
                        // preposterous
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
            if (mModuleList.TryGetValue(hash, out list)) {
                foreach (var m in list) {
                    aComponentList.Add((S)m);
                }
                return true;
            }
            return false;
        }
    }
}