using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class Logger
    {
        string nl => Environment.NewLine;
        readonly StringBuilder mWork = new StringBuilder();
        readonly StringBuilder mLog = new StringBuilder();
        readonly List<string> mPersistent = new List<string>();

        public void log(MyDetectedEntityInfo e) => log(string4(e));
        
        string string4(MyDetectedEntityInfo e) {
            mWork.Clear();
            mWork.AppendLine($"{e.Name} {e.Type}");
            mWork.AppendLine(e.EntityId.ToString());
            mWork.Append(string4(e.Orientation.Up));
            return mWork.ToString();
        }
        public void log(Vector3D v) => log(string4(v));
        string string4(Vector3D v) => $"X {v.X}{nl}Y {v.Y}{nl}Z {v.Z}";
        
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
        public void persist(MyDetectedEntityInfo e) => persist(string4(e));
        public void persist(string aMessage) {
            if (false && mPersistent.Count > 15) {
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
