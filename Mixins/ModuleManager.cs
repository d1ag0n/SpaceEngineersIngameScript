using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public static class ModuleManager {
        static readonly Dictionary<string, List<IMyTerminalBlock>> mTags = new Dictionary<string, List<IMyTerminalBlock>>();

        public static LogModule logger { get; private set; }
        public static ShipControllerModule controller { get; private set; }
        static readonly HashSet<long> mRegistry = new HashSet<long>();
        static readonly List<IMyEntity> mBlocks = new List<IMyEntity>();
        static readonly List<IAccept> mModules = new List<IAccept>();
        
        static readonly Dictionary<int, List<IAccept>> mModuleList = new Dictionary<int, List<IAccept>>();
        static readonly Dictionary<long, List<IMyCubeBlock>> mGridBlocks = new Dictionary<long, List<IMyCubeBlock>>();
        public static MyGridProgram Program { get; private set; }
        public static Menu MainMenu(MenuModule aMain) => new Menu(aMain, mModules);
        public static void Update() {
            try {
                controller.Update();
                for (int i = 2; i < mModules.Count; i++) {
                    var mb = mModules[i] as ModuleBase;
                    if (mb.Active) {
                        mb.Update();
                    }
                }
            } catch (Exception ex) {
                logger.persist(ex.ToString());
            }
            logger.Update();
        }
        public static void Initialize(MyGridProgram aProgram = null) {
            if (Program == null) {
                if (aProgram == null) {
                    throw new ArgumentException("Program cannot be null.");
                }
                Program = aProgram;
                logger = new LogModule();
                controller = new ShipControllerModule();
            } else {
                foreach (var list in mTags.Values) {
                    list.Clear();
                }
                foreach (var list in mGridBlocks.Values) {
                    list.Clear();
                }
                mRegistry.Clear();
                mBlocks.Clear();
            }
            
            Program.GridTerminalSystem.GetBlocksOfType(mBlocks, block => mRegistry.Add(block.EntityId));
            
            foreach (var block in mBlocks) {
                initTags(block as IMyTerminalBlock);                
                addByGrid(block as IMyCubeBlock);
            }
            
            foreach (var module in mModules) {
                foreach (var block in mBlocks) {
                    if (block is IMyTerminalBlock) {
                        module.Accept(block as IMyTerminalBlock);
                    }
                }
            }
        }
        static void addByGrid(IMyCubeBlock aBlock) {
            List<IMyCubeBlock> list;
            if (aBlock != null) {
                if (!mGridBlocks.TryGetValue(aBlock.CubeGrid.EntityId, out list)) {
                    list = new List<IMyCubeBlock>();
                    mGridBlocks.Add(aBlock.CubeGrid.EntityId, list);
                }
                list.Add(aBlock);
            }
        }
        static void initTags(IMyTerminalBlock aBlock) {
            if (null != aBlock) {
                var tags = getTags(aBlock);
                for (int i = 0; i < tags.Length; i++) {
                    var tag = tags[i].Trim().ToLower();
                    List<IMyTerminalBlock> list;
                    if (mTags.ContainsKey(tag)) {
                        list = mTags[tag];
                    } else {
                        mTags[tag] = list = new List<IMyTerminalBlock>();
                    }
                    if (!list.Contains(aBlock)) {
                        list.Add(aBlock);
                    }
                }
            }
        }
        static string[] getTags(IMyTerminalBlock aBlock) =>
            aBlock.CustomData.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        public static bool HasTag(IMyTerminalBlock aBlock, string aTag) {
            var tags = getTags(aBlock);
            for (int i = 0; i < tags.Length; i++) if (tags[i] == aTag) return true;
            return false;
        }
        public static bool GetByGrid<T>(long grid, ref T block) {
            List<IMyCubeBlock> list;
            if (mGridBlocks.TryGetValue(grid, out list)) {
                foreach (var b in list) {
                    if (b is T) {
                        block = (T)b;
                        return true;
                    }
                }
            }
            return false;
        }
        public static void Add<T>(Module<T> aModule) {
            
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
                aModule.Accept(block as IMyTerminalBlock);
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