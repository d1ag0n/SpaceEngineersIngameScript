using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Lag : IUpdate
    {
        public double Value { get; private set; }
        bool accurate;
        double[] times;
        double sum = 0;
        int pos = 0;
        
        public Lag(int samples) {
            times = new double[samples];
        }
        
        public void Update(double time) {
            sum -= times[pos];
            times[pos] = time;
            sum += time;            
            pos++;
            if (pos == times.Length) {
                pos = 0;
                accurate = true;
            }
            Value = accurate ? sum / times.Length : sum / pos;
        }
    }
    class V3DLag
    {
        bool accurate;
        Vector3D[] values;
        Vector3D sum;
        int pos = 0;
        public V3DLag(int samples) {
            values = new Vector3D[samples];
        }
        public Vector3D update(Vector3D value) {
            sum -= values[pos];
            values[pos] = value;
            sum += value;
            pos++;
            if (pos == values.Length) {
                pos = 0;
                accurate = true;
            }
            return accurate ? sum / values.Length : sum / pos;
        }
    }
}
