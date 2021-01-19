using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace Library
{
    class GTS
    {
        readonly MyGridProgram program;
        Dictionary<string, IMyTerminalBlock> mBlocks;
        public GTS(MyGridProgram aProgram) {
            program = aProgram;
            init();
        }
        public T get<T>(string aName) {
            IMyTerminalBlock result;
            if (!mBlocks.TryGetValue(aName.ToLower(), out result)) {
                result = null;
            }
            return (T)result;
        }
        public void initBlockList<T>(string aName, out T[] array) {
            var list = new List<T>();
            int i = 0;
            T t;
            while (null != (t = get<T>(aName + i.ToString()))) {
                list.Add(t);
                i++;
            }
            array = list.ToArray();
        }
        public void getByType<T>(T t, out T[] array) {
            List<T> list = new List<T>();
            foreach (var b in mBlocks.Values) {
                if (b is T) {
                    list.Add((T)b);
                }
            }
            array = list.ToArray();
        }
        private void init() {
            List<IMyTerminalBlock> blocks;
            if (null == mBlocks) {
                blocks = new List<IMyTerminalBlock>();
                mBlocks = new Dictionary<string, IMyTerminalBlock>();
            } else {
                blocks = new List<IMyTerminalBlock>(mBlocks.Count);
                mBlocks = new Dictionary<string, IMyTerminalBlock>(mBlocks.Count);
            }
            program.GridTerminalSystem.GetBlocks(blocks);
            for (int i = blocks.Count - 1; i > -1; i--) {
                var block = blocks[i];
                if (block.CubeGrid == program.Me.CubeGrid) {
                    var name = block.CustomName.ToLower();
                    if (mBlocks.ContainsKey(name)) {
                        throw new Exception($"Duplicate block name '{name}' is prohibited.");
                    }
                    mBlocks.Add(name, block);
                }
            }
        }
    }
}
