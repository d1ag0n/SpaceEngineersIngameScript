using Sandbox.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    
    class Stringerator : IEnumerator<string> {
        int Position;
        string[] Values;

        public Stringerator(string[] aValues) {
            Values = aValues;
        }
        public string Current => Values[Position];

        object IEnumerator.Current => Current;

        public void Dispose() {}

        public bool MoveNext() {
            if (Position == Values.Length - 1) {
                return false;
            }
            Position++;
            return true;
        }

        public void Reset() => Position = 0;
    }
}