using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Logger
    {
        readonly StringBuilder mLog = new StringBuilder();
        readonly List<string> mPersistent = new List<string>();
        public void log(double d) => log();
        public void log(MyDetectedEntityInfo e) {
            log(e.Name);
        }
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
            string result = get();
            mLog.Clear();
            return result;
        }
        public void removeP(int index) {
            if (mPersistent.Count > index) {
                mPersistent.RemoveAt(index);
            }
        }
        public void persist(string aMessage) {
            if (mPersistent.Count > 15) {
                mPersistent.RemoveAt(0);
            }
            mPersistent.Add(aMessage);
        }
        public string get() {
            for (int i = 0; i < mPersistent.Count; i++) {
                mLog.Insert(0, Environment.NewLine);
                mLog.Insert(0, mPersistent[i]);
                mLog.Insert(0, $"#{i} ");
            }
            return mLog.ToString();
        }

    }
}
