using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class ModuleManager {
        readonly HashSet<long> mRegistry = new HashSet<long>();
        readonly List<IMyTerminalBlock> mBlocks = new List<IMyTerminalBlock>();
        readonly List<ModuleBase> mModules = new List<ModuleBase>();
        readonly List<ModuleBase> mInputModules = new List<ModuleBase>();
        readonly List<ModuleBase> mUpdateModules = new List<ModuleBase>();
        readonly Dictionary<int, List<ModuleBase>> mModuleList = new Dictionary<int, List<ModuleBase>>();
        readonly Dictionary<long, List<IMyTerminalBlock>> mGridBlocks = new Dictionary<long, List<IMyTerminalBlock>>();
        readonly Lag mLag = new Lag(18);
        readonly Dictionary<string, List<IMyTerminalBlock>> mTags = new Dictionary<string, List<IMyTerminalBlock>>();

        public readonly bool LargeGrid;
        public readonly Program mProgram;
        public readonly ShipControllerModule mController;
        public readonly LogModule mLog;

        public bool Mother;
        public bool Probe;
        public bool Drill;        
        public string UserInput = "DEFAULT";
        

        public double Lag => mLag.Value;
        public double Runtime { get; private set; }

        public ModuleManager(Program aProgram) {
            mProgram = aProgram;
            mLog = new LogModule(this);
            LargeGrid = aProgram.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            mProgram.Me.CustomName = "!Smart Pilot";
            mController = new ShipControllerModule(this);
        }
        /// <summary>
        /// https://discord.com/channels/125011928711036928/216219467959500800/755140967517913148
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        float GetTerminalBlockHealth(IMyTerminalBlock block) {
            IMySlimBlock slimblock = block.CubeGrid.GetCubeBlock(block.Position);
            float maxIntegrity = slimblock.MaxIntegrity;
            float buildIntegrity = slimblock.BuildIntegrity;
            float currentDamage = slimblock.CurrentDamage;
            float health = (buildIntegrity - currentDamage) / maxIntegrity;
            return health;
        }

        

        // todo menu module with menus added to it
        //public Menu MainMenu(MenuModule aMain) => new Menu(aMain, mModules);

        public void Update(string arg, UpdateType aType) {
            try {
                
                Runtime += mProgram.Runtime.TimeSinceLastRun.TotalSeconds;
                mLag.Update(mProgram.Runtime.LastRunTimeMs);
                if ((aType & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
                    if (arg.Length > 0) {
                        if (mController.OnMission) {
                            mController.mMission.Input(arg);
                        }
                        foreach (var m in mInputModules) {
                            m.onInput(arg);
                        }
                    }
                }
                if ((aType & UpdateType.Update10) != 0) {
                    mLog.log(mLag.Value, " - ", DateTime.Now.ToString());
                    for (int i = 0; i < mUpdateModules.Count; i++) {
                        var m = mUpdateModules[i];
                        try {
                            m.onUpdate?.Invoke();
                        } catch (Exception ex) {
                            mLog.persist(m.ToString() + ex.ToString());
                        }
                    }
                    mLog.onUpdate();
                }
            } catch (Exception ex) {
                mLog.persist(ex.ToString());
            }
        }
        // todo Persistence Class
        /*
        */
        // todo persistence class

        
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
                        if (module is IModuleBlock) {
                            module.Accept(block);
                        }
                    }
                }
                return false;
            });
            
            for (int i = 1; i < mModules.Count; i++) {
                var module = mModules[i];
                if (module.onInput != null) {
                    mInputModules.Add(module);
                }
                if (module.onUpdate != null) {
                    mUpdateModules.Add(module);
                }
            }

            // todo move up into GTS loop, need to change module initialization behavior first,
            // because modules may look for additionals that mey not yet be loaded
            // when reinitialization is implemented this should be addressed

        }
        public void Add(ModuleBase aModule) {
            List<ModuleBase> list;

            var type = aModule.GetType();
            var hash = type.GetHashCode();

            if (!mModuleList.TryGetValue(hash, out list)) {
                mModuleList.Add(hash, list = new List<ModuleBase>());
            }

            list.Add(aModule);

            if (aModule is IModuleBlock) {
                if (mBlocks.Count > 0) {
                    foreach (var block in mBlocks) {
                        aModule.Accept(block);
                    }
                }
            }
            
            
            mModules.Add(aModule);
        }
        public void getByType<T>(List<T> blocks) {
            foreach (var block in mBlocks) {
                if (block is T) {
                    blocks.Add((T)block);
                }
            }
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

        public void GetAllByGrid<T>(long grid, List<T> blocks) {
            List<IMyTerminalBlock> list;
            if (mGridBlocks.TryGetValue(grid, out list)) {
                foreach (var b in list) {
                    if (b is T) {
                        blocks.Add((T)b);
                    }
                }
            }
        }
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