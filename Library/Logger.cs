using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Logger
    {
        StringBuilder mLog = new StringBuilder();
        public void log(double d) => log();
        public void log(Vector3D v) => log("X ", v.X, null, "Y ", v.Y, null, "Z ", v.Z);
        public void log(params object[] args) {
            if (null != args) {
                for (int i = 0; i < args.Length; i++) {
                    var arg = args[i];
                    if (null == arg) {
                        mLog.AppendLine();
                    } else if (arg is Vector3D) {
                        mLog.AppendLine();
                        log((Vector3D)arg);
                    } else if (arg is double) {
                        mLog.Append(((double)arg).ToString("N"));
                    } else {
                        mLog.Append(arg.ToString());
                    }
                }
            }
            mLog.AppendLine();
        }
        public string clear() {
            string result = mLog.ToString();
            mLog = new StringBuilder();
            return result;
        }
        public string get() => mLog.ToString();
    }
}
