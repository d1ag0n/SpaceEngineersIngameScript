using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class ModuleManager {
        public readonly MyGridProgram mProgram;
        readonly Lag mLag = new Lag(6);
        
        public double Runtime { get; private set; }

        public readonly IGC mIGC;
        public readonly bool LargeGrid;

        public bool Mother;
        public bool Probe;
        public bool Drill;

        public ModuleManager(MyGridProgram aProgram) {
            mProgram = aProgram;
            logger = new LogModule(this);
            
            LargeGrid = aProgram.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            mProgram.Me.CustomName = "!Smart Pilot";

            mIGC = new IGC(mProgram.IGC);
            controller = new ShipControllerModule(this);
        }

        
        public double Lag => mLag.Value;
        public string UserInput = "DEFAULT";
        readonly Dictionary<string, List<IMyTerminalBlock>> mTags = new Dictionary<string, List<IMyTerminalBlock>>();

        public LogModule logger { get; private set; }
        public ShipControllerModule controller { get; private set; }

        readonly HashSet<long> mRegistry = new HashSet<long>();
        readonly List<IMyTerminalBlock> mBlocks = new List<IMyTerminalBlock>();
        readonly List<ModuleBase> mModules = new List<ModuleBase>();

        readonly Dictionary<int, List<ModuleBase>> mModuleList = new Dictionary<int, List<ModuleBase>>();
        readonly Dictionary<long, List<IMyTerminalBlock>> mGridBlocks = new Dictionary<long, List<IMyTerminalBlock>>();

        public Menu MainMenu(MenuModule aMain) => new Menu(aMain, mModules);

        public void Update(string arg, UpdateType type) {
            Runtime += mProgram.Runtime.TimeSinceLastRun.TotalSeconds;

            mLag.Update(mProgram.Runtime.LastRunTimeMs);

            if ((type & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
                if (arg.Length > 0) {
                    if (arg == "save") {
                        Save();
                    } else {
                        MenuModule menu;
                        if (GetModule(out menu)) {
                            menu.Input(arg);
                        }
                    }
                }
            }

            if ((type & UpdateType.IGC) != 0) {
                mIGC.MailCall(Runtime);
            }

            if ((type & UpdateType.Update10) != 0) {
                logger.log(mLag.Value, " - ", DateTime.Now.ToString());
                for (int i = 1; i < mModules.Count; i++) {
                    try {
                        mModules[i].onUpdate?.Invoke();
                    } catch (Exception ex) {
                        logger.persist(ex.ToString());
                    }
                }
                logger.onUpdate();
            }
        }
        public string Save() {
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
        public void Load(string aStorage) {
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
            foreach (var m in mModules) {
                if (moduleEntries.TryGetValue(m.GetType().ToString(), out work)) {
                    foreach (var data in work) {
                        m.onLoad(s, data);
                    }
                }
            }
        }
        public void Initialize() {
            foreach (var list in mTags.Values) {
                list.Clear();
            }
            foreach (var list in mGridBlocks.Values) {
                list.Clear();
            }
            
            mRegistry.Clear();
            mBlocks.Clear();
            
            mProgram.GridTerminalSystem.GetBlocksOfType(mBlocks, block => {
                addByGrid(block);
                if (block.IsSameConstructAs(mProgram.Me) && mRegistry.Add(block.EntityId)) {
                    mBlocks.Add(block);
                    initTags(block);
                    foreach (var module in mModules) {
                        module.Accept(block);
                    }
                }
                return false;
            });

            // todo move up into GTS loop, need to change module initialization behavior first,
            // because modules may look for additionals that mey not yet be loaded
            // when reinitialization is implemented this should be addressed
            
        }
        public void getByTag<T>(string aTag, ref T aBlock) {
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

        void addByGrid(IMyTerminalBlock aBlock) {
            List<IMyTerminalBlock> list;
            if (aBlock != null) {
                if (!mGridBlocks.TryGetValue(aBlock.CubeGrid.EntityId, out list)) {
                    list = new List<IMyTerminalBlock>();
                    mGridBlocks.Add(aBlock.CubeGrid.EntityId, list);
                }
                list.Add(aBlock);
            }
        }
        void initTags(IMyTerminalBlock aBlock) {
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
        string[] getTags(IMyTerminalBlock aBlock) =>
           aBlock.CustomData.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        public bool HasTag(IMyTerminalBlock aBlock, string aTag) {
            var tags = getTags(aBlock);
            for (int i = 0; i < tags.Length; i++) if (tags[i] == aTag) return true;
            return false;
        }

        public bool ConnectedGrid(long entityId) => mGridBlocks.ContainsKey(entityId);

        public bool GetByGrid<T>(long grid, ref T block) {
            List<IMyTerminalBlock> list;
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
        public void Add<T>(Module<T> aModule) {
            List<ModuleBase> list;

            var type = aModule.GetType();
            var hash = type.GetHashCode();

            if (!mModuleList.TryGetValue(hash, out list)) {
                list = new List<ModuleBase>();
                mModuleList.Add(hash, list);
            }
            list.Add(aModule);
            mModules.Add(aModule);
            foreach (var block in mBlocks) {
                aModule.Accept(block as IMyTerminalBlock);
            }
        }
        public bool GetModule<S>(out S aComponent) where S : class {
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
        public bool GetModules<S>(List<S> aComponentList) where S : class {
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