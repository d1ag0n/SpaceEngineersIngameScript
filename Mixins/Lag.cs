using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Lag
    {
        double[] times;
        double sum = 0;
        int pos = 0;
        public Lag(int samples) {
            times = new double[samples];
        }
        public double update(double runtime) {
            sum -= times[pos];
            times[pos] = runtime;
            sum += runtime;            
            pos++;
            if (pos == times.Length) {
                pos = 0;
            }
            return sum / times.Length;
        }
    }
    class V3DLag
    {
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
            }
            return sum / values.Length;
        }
    }
}
