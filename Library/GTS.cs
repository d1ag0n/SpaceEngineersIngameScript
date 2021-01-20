using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace Library
{
    class GTS {
        readonly MyGridProgram program;
        readonly string msMasterTag;

        Dictionary<string, IMyTerminalBlock> mBlocks;
        Dictionary<string, List<IMyTerminalBlock>> mTags;
        
        public GTS(MyGridProgram aProgram, string aMasterTag = null) {
            program = aProgram;
            msMasterTag = aMasterTag;
            init();
        }

        public T get<T>(string aName) {
            IMyTerminalBlock result;
            if (!mBlocks.TryGetValue(aName.ToLower(), out result)) {
                result = null;
            }
            return (T)result;
        }

        public void getByTag<T>(string aTag, List<T> aList) {
            List<IMyTerminalBlock> list;
            
            if (mTags.TryGetValue(aTag, out list)) {
                for (int i = 0; i < list.Count; i++) {
                    var b = list[i];
                    if (b is T) {
                        aList.Add((T)b);
                    }
                }
            }
        }

        void initTags(IMyTerminalBlock aBlock) {
            if (null != aBlock && null != aBlock.CustomData) {
                var tags = aBlock.CustomData.Split('#');
                for (int i = 0; i < tags.Length; i++) {
                    var tag = tags[i].Trim().ToLower();

                    if (null != msMasterTag && msMasterTag != tag) {
                        break;
                    }

                    List<IMyTerminalBlock> list;
                    if (mTags.ContainsKey(tag)) {
                        list = new List<IMyTerminalBlock>();
                    } else {
                        mTags[tag] = list = new List<IMyTerminalBlock>();
                    }
                    if (!list.Contains(aBlock)) {
                        list.Add(aBlock);
                    }
                }
            }
        }

        public void initBlockList<T>(List<T> aList) {
            foreach (var b in mBlocks.Values) {
                if (b is T) {
                    aList.Add((T)b);
                }
            }
        }

        public void initBlockList<T>(string aName, List<T> aList) {
            int i = 0;
            T t;
            while (null != (t = get<T>(aName + i.ToString()))) {
                aList.Add(t);
                i++;
            }
        }

        public void init() {
            List<IMyTerminalBlock> blocks;
            if (null == mBlocks) {
                blocks = new List<IMyTerminalBlock>();
                mBlocks = new Dictionary<string, IMyTerminalBlock>();
            } else {
                blocks = new List<IMyTerminalBlock>(mBlocks.Count);
                mBlocks = new Dictionary<string, IMyTerminalBlock>(mBlocks.Count);
            }
            mTags = new Dictionary<string, List<IMyTerminalBlock>>();
            program.GridTerminalSystem.GetBlocks(blocks);
            for (int i = blocks.Count - 1; i > -1; i--) {
                var block = blocks[i];
                if (block.CubeGrid == program.Me.CubeGrid) {
                    var name = block.CustomName.ToLower();
                    if (mBlocks.ContainsKey(name)) {
                        throw new Exception($"Duplicate block name '{name}' is prohibited.");
                    }
                    mBlocks.Add(name, block);
                    initTags(block);
                }
            }
        }
    }
}
