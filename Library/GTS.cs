using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    class GTS {
        readonly MyGridProgram program;
        Dictionary<long, IMyTerminalBlock> mBlocks;
        Dictionary<string, List<IMyTerminalBlock>> mTags;
        Dictionary<long, List<string>> mArguments;
        readonly Logger g;
        
        public GTS(MyGridProgram aProgram, Logger aLogger) {
            program = aProgram;
            g = aLogger;
            init();
        }
        string[] getTags(IMyTerminalBlock aBlock) {
            var tags = aBlock.CustomData.Split("#".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            /*foreach (var tag in tags) {
                var args = tag.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }*/
            return tags;
        }
        void initTags(IMyTerminalBlock aBlock) {
            if (null != aBlock && null != aBlock.CustomData) {
                var tags = getTags(aBlock);
                //g.log("tags found ", tags.Length);
                for (int i = 0; i < tags.Length; i++) {
                    var tag = tags[i].Trim().ToLower();
                    
                    //g.log("#", tag);
                    List<IMyTerminalBlock> list;
                    if (mTags.ContainsKey(tag)) {
                        list = mTags[tag];
                    } else {
                        mTags[tag] = list = new List<IMyTerminalBlock>();
                    }
                    if (!list.Contains(aBlock)) {
                        //g.log("adding block with tag #", tag);
                        list.Add(aBlock);
                    }
                }
            }
        }

        public bool get<T>(ref T aBlock) {
            foreach (var b in mBlocks.Values) 
                if (b is T) {
                    aBlock = (T)b;
                    return true;
                }
            return false;
        }
        public void getByTag<T>(string aTag, ref T aBlock) {
            List<IMyTerminalBlock> list;

            if (mTags.TryGetValue(aTag, out list)) {
                if (list.Count > 0) {
                    aBlock = (T)list[0];
                }
            }
        }
        public void initListByTag<T>(string aTag, List<T> aList, bool aClearList = true) {
            List<IMyTerminalBlock> list;
            if (aClearList) {
                aList.Clear();
            }
            if (mTags.TryGetValue(aTag, out list)) {
                //g.log("found list for #", aTag, " with count ", list.Count);
                for (int i = 0; i < list.Count; i++) {
                    var b = list[i];
                    if (b is T) {
                        g.log("adding ", b.CustomName);
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
        public bool hasTag(IMyTerminalBlock aBlock, string aTag) {
            var tags = getTags(aBlock);
            for (int i = 0; i < tags.Length; i++) if (tags[i] == aTag) return true;
            return false;
        }

        public void init() {
            List<IMyTerminalBlock> blocks;
            if (null == mBlocks) {
                blocks = new List<IMyTerminalBlock>();
                mBlocks = new Dictionary<long, IMyTerminalBlock>();
            } else {
                blocks = new List<IMyTerminalBlock>(mBlocks.Count);
                mBlocks = new Dictionary<long, IMyTerminalBlock>(mBlocks.Count);
            }
            mTags = new Dictionary<string, List<IMyTerminalBlock>>();
            program.GridTerminalSystem.GetBlocks(blocks);
            g.log("GTS init blocks found ", blocks.Count);
            for (int i = 0; i < blocks.Count; i++) {
                var block = blocks[i];
                if (block.IsSameConstructAs(program.Me)) {
                    
                    //g.log("adding block ", block.CustomName, " ", block.CustomData);
                    mBlocks.Add(block.EntityId, block);
                    initTags(block);
                } else {
                    //g.log("Not on this grid.");
                }
            }
        }
    }
}
