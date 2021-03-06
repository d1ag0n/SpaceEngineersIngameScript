﻿using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    class GTS {
        
        readonly Dictionary<long, IMyEntity> mBlocks = new Dictionary<long, IMyEntity>();
        //readonly Dictionary<string, List<IMyTerminalBlock>> mTags = new Dictionary<string, List<IMyTerminalBlock>>();
        //readonly Dictionary<long, Dictionary<string, List<string>>> mArguments = new Dictionary<long, Dictionary<string, List<string>>>();
        readonly List<IMyEntity> mWork = new List<IMyEntity>();
        readonly Dictionary<long, List<IMyCubeBlock>> mGridBlocks = new Dictionary<long, List<IMyCubeBlock>>();
        
        public GTS(MyGridProgram aProgram, Logger aLogger) {
            program = aProgram;
            g = aLogger;
            init();
        }
        /*public string[] getTags(IMyTerminalBlock aBlock) {
            var tags = aBlock.CustomData.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tags.Length; i++) {
                var tag = tags[i] = tags[i].Trim();
                var args = tag.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0) {
                    var argList = getTagArgs(aBlock.EntityId, tag, true);
                    foreach (var sarg in args) {
                        var arg = sarg.Trim();
                        if (!argList.Contains(arg)) {
                            argList.Add(arg);
                        }
                    }
                }
            }
            return tags;
        }*/
        public List<string> getTagArgs(long aBlock, string aTag, bool aGenerate = false) {
            Dictionary<string, List<string>> top;
            if (!mArguments.TryGetValue(aBlock, out top)) {
                top = new Dictionary<string, List<string>>();
                mArguments.Add(aBlock, top);
            }
            List<string> result = null;
            if (!top.TryGetValue(aTag, out result) && aGenerate) {
                result = new List<string>();
                top.Add(aTag, result);
            }
            return result;
        }
        /*void initTags(IMyTerminalBlock aBlock) {
            if (null != aBlock && null != aBlock.CustomData) {
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
        }*/
        public bool get<T>(ref T aBlock) {
            foreach (var b in mBlocks.Values) 
                if (b is T) {
                    aBlock = (T)b;
                    return true;
                }           
            return false;
        }
        /*public void getByTag<T>(string aTag, ref T aBlock) {
            List<IMyTerminalBlock> list;
            if (mTags.TryGetValue(aTag.ToLower(), out list)) {
                foreach (var b in list) {
                    if (b is T) {
                        aBlock = (T)b;
                        return;
                    }
                }
            }
        }*/
        public void initListByTag<T>(string aTag, List<T> aList, bool aClearList = true) {
            List<IMyTerminalBlock> list;
            if (aClearList) {
                aList.Clear();
            }
            if (mTags.TryGetValue(aTag, out list)) {
                for (int i = 0; i < list.Count; i++) {
                    var b = list[i];
                    if (b is T) {
                        //g.log("adding ", b.CustomName);
                        aList.Add((T)b);
                    } else {
                        g.log("not added ", b, " ", b.CustomName);
                    }
                }
            } else {
                //g.log("list not found for #", aTag);
            }
        }        
        public void initList<T>(List<T> aList, bool aClearList = true) {
            if (aClearList) aList.Clear();
            foreach (var b in mBlocks.Values) if (b is T) aList.Add((T)b);
        }
        
        /*public bool hasTag(IMyTerminalBlock aBlock, string aTag) {
            var tags = getTags(aBlock);
            for (int i = 0; i < tags.Length; i++) if (tags[i] == aTag) return true;
            return false;
        }*/
        /*
        public void init() {
            mTags.Clear();
            mBlocks.Clear();
            mArguments.Clear();
            program.GridTerminalSystem.GetBlocksOfType(mWork);
            foreach (var b in mWork) { 
                if (b is IMyTerminalBlock) {
                    var tb = b as IMyTerminalBlock;
                    if (tb.IsSameConstructAs(program.Me)) {
                        mBlocks.Add(b.EntityId, tb);
                        initTags(tb);
                    }
                }
                if (b is IMyCubeBlock)
                    addByGrid(b as IMyCubeBlock);
            }
            mWork.Clear();
        }*/

        /*public void getByGrid<T>(long grid, ref T block) {
            List<IMyCubeBlock> list;
            if (mGridBlocks.TryGetValue(grid, out list)) {
                foreach(var b in list) {
                    if (b is T) {
                        block = (T)b;
                        return;
                    }
                }
            }
        }*/

        /*void addByGrid(IMyCubeBlock b) {
            List<IMyCubeBlock> list;
            if (!mGridBlocks.TryGetValue(b.CubeGrid.EntityId, out list)) {
                list = new List<IMyCubeBlock>();
                mGridBlocks.Add(b.CubeGrid.EntityId, list);
            }
            list.Add(b);
        }*/

    }
}
