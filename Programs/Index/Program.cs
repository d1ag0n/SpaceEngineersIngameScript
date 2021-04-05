using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Index {
    class Program {
        readonly Dictionary<int, IndexEntry> mIndex = new Dictionary<int, IndexEntry>();
        readonly List<IndexEntry> mIndexList = new List<IndexEntry>();
        const int size = 1024;
        byte[] mData = new byte[size];
        int position = 0;
        static void Main(string[] args) {
            var enc = Encoding.ASCII;
            // chunk size
            // number of addresses
            // many addresses
            // int index, 64 bytes of
            var bytes = new char[256];
            for (byte i = 0; i != 255; i++) {
                bytes[i] = (char)i;
            }
            ImmutableArray.Create()
            var str = BitConverter.ToString(bytes);
            var otherBytes = enc.GetBytes(str);
            for (byte i = 0; i < 256; i++) {
                if (bytes[i] != otherBytes[i]) {
                    throw new Exception();
                }
            }
            return;

        }
        bool AddData(string name, byte[] data) {
            int index = name.GetHashCode();
            if (mIndex.ContainsKey(index)) {
                return false;
            }
            while (mData.Length - position < data.Length) {
                var bytes = new byte[mData.Length + size];
                mData.CopyTo(bytes, 0);
                bytes = mData;
            }
            return false;
        }
        
        class IndexEntry {
            public readonly int Index;
            public readonly int Start;
            public readonly int Length;

            public IndexEntry(int index, int start, int length) {
                Index = index;
                Start = start;
                Length = length;
            }
        }

    }
}
