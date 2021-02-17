using System;
using System.Collections.Generic;
using System.Text;

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
}
