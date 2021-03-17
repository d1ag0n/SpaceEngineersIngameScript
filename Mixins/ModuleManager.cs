using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public static class ModuleManager {
        static readonly Lag mLag = new Lag(90);
        public static double Lag => mLag.Last;
        public static string UserInput = "DEFAULT";
        static readonly Dictionary<string, List<IMyTerminalBlock>> mTags = new Dictionary<string, List<IMyTerminalBlock>>();

        public static LogModule logger { get; private set; }
        public static ShipControllerModule controller { get; private set; }

        static readonly HashSet<long> mRegistry = new HashSet<long>();
        static readonly List<IMyCubeBlock> mBlocks = new List<IMyCubeBlock>();
        static readonly List<ModuleBase> mModules = new List<ModuleBase>();
        
        static readonly Dictionary<int, List<ModuleBase>> mModuleList = new Dictionary<int, List<ModuleBase>>();
        static readonly Dictionary<long, List<IMyCubeBlock>> mGridBlocks = new Dictionary<long, List<IMyCubeBlock>>();
        public static MyGridProgram Program { get; private set; }
        public static Menu MainMenu(MenuModule aMain) => new Menu(aMain, mModules);
        
        public static void Update() {
            mLag.update(Program.Runtime.LastRunTimeMs);
            try {
                logger.log(DateTime.Now.ToString());
                for (int i = 1; i < mModules.Count; i++) {
                    mModules[i].onUpdate?.Invoke();
                }
            } catch (Exception ex) {
                logger.persist(ex.ToString());
            }
            logger.onUpdate();
        }
        public static string Save() {
            var s = new Serialize();
            var one = false;
            foreach (var m in mModules) {

                var mb = m as ModuleBase;
                if (mb.onSave != null) {
                    if (one) {
                        s.mod();
                    }
                    s.grp(mb.GetType().ToString());
                    mb.onSave(s);
                    one = true;
                }

            }
            return s.Clear();
        }
        public static void Load(string aStorage) {
            var s = new Serialize();
            var moduleEntries = new Dictionary<string, List<string>>();
            List<string> work;
            var mods = aStorage.Split(Serialize.MODSEP);
            foreach (var mod in mods) {
                var grps = mod.Split(Serialize.GRPSEP);
                if (!moduleEntries.TryGetValue(grps[0], out work)) {
                    work = new List<string>();
                    moduleEntries.Add(grps[0], work);
                }
                work.Add(grps[1]);
            }
            foreach(var m in mModules) {                
                if (moduleEntries.TryGetValue(m.GetType().ToString(), out work)) {
                    foreach(var data in work) {
                        m.onLoad(s, data);
                    }
                }
            }
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
            Program.GridTerminalSystem.GetBlocksOfType(mBlocks, block => {
                if (block.CubeGrid == Program.Me.CubeGrid && mRegistry.Add(block.EntityId)) {
                    mBlocks.Add(block);
                    initTags(block as IMyTerminalBlock);
                    controller.Accept(block as IMyTerminalBlock);
                }
                addByGrid(block);
                return false;
            });
            
            // todo move up into GTS loop, need to change module initialization behavior first,
            // because modules may look for additionals that mey not yet be loaded
            // when reinitialization is implemented this should be addressed
            foreach (var module in mModules) {
                if (module == controller) {
                    continue;
                }
                foreach (var block in mBlocks) {
                    if (block is IMyTerminalBlock) {
                        module.Accept(block as IMyTerminalBlock);
                    }
                }
            }
        }
        public static void getByTag<T>(string aTag, ref T aBlock) {
            List<IMyTerminalBlock> list;
            if (mTags.TryGetValue(aTag.ToLower(), out list)) {
                foreach (var b in list) {
                    if (b is T) {
                        aBlock = (T)b;
                        return;
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
            List<ModuleBase> list;

            var type = aModule.GetType();
            var hash = type.GetHashCode();

            if (!mModuleList.TryGetValue(hash, out list)) {
                list = new List<ModuleBase>();
                mModuleList.Add(hash, list);
            }
            list.Add(aModule);
            mModules.Add(aModule);
            foreach(var block in mBlocks) {
                aModule.Accept(block as IMyTerminalBlock);
            }
        }
        public static bool GetModule<S>(out S aComponent) where S : class {
            var hash = typeof(S).GetHashCode();
            List<ModuleBase> list;
            if (mModuleList.TryGetValue(hash, out list)) {
                foreach (var m in list) {
                    if (m is S) {
                        aComponent = m as S;
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
        public static bool GetModules<S>(List<S> aComponentList) where S : class {
            var hash = typeof(S).GetHashCode();
            List<ModuleBase> list;
            if (mModuleList.TryGetValue(hash, out list)) {
                foreach (var m in list) {
                    aComponentList.Add(m as S);
                }
                return true;
            }
            return false;
        }
    }
}