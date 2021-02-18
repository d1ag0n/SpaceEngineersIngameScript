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
        string string4(BoundingBoxD b) {
            mWork.Clear();
            mWork.AppendLine(b.Min.ToString());
            mWork.AppendLine(b.Center.ToString());
            mWork.AppendLine(b.Max.ToString());
            return mWork.ToString();
        }
        string string4(MyDetectedEntityInfo e) {
            
            mWork.Clear();
            mWork.AppendLine($"{e.Name} {e.Type} {e.EntityId}");
            //mWork.AppendLine(e.EntityId.ToString());
            
            
            //mWork.AppendLine(gps("BoxMin", e.BoundingBox.Min));
            //mWork.AppendLine(gps("BoxCenter", e.BoundingBox.Center));
            //mWork.AppendLine(gps("BoxMax", e.BoundingBox.Max));
            var bb = e.BoundingBox;
            int i = 1;
            foreach (var c in bb.GetCorners()) {
                //mWork.AppendLine(gps("corner" + i++, c));
            }
            //mWork.AppendLine("Translation");
            //mWork.AppendLine(string4(e.Orientation.Translation));
            //mWork.AppendLine("Up");
            //mWork.AppendLine(string4(e.Orientation.Up));
            if (e.HitPosition.HasValue) {
                //mWork.AppendLine("hit");
                mWork.AppendLine(gps("hit", e.HitPosition.Value));
            }
            return mWork.ToString();
        }
      
        public void log(Vector3D v) => log(string4(v));
        string string4(Vector3D v) => $"X {v.X}{nl}Y {v.Y}{nl}Z {v.Z}";
        public string gps(string aName, Vector3D aPos) {
            // GPS:ARC_ABOVE:19680.65:144051.53:-109067.96:#FF75C9F1:
            var sb = new StringBuilder("GPS:");
            sb.Append(aName);
            sb.Append(":");
            sb.Append(aPos.X.ToString("F2"));
            sb.Append(":");
            sb.Append(aPos.Y.ToString("F2"));
            sb.Append(":");
            sb.Append(aPos.Z.ToString("F2"));
            sb.Append(":#FFFF00FF:");
            return sb.ToString();
        }
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
                        mLog.Append(((double)arg).ToString("F3"));
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

        public void persist(Vector3D v) => persist(string4(v));
        public void persist(BoundingBoxD b) => persist(string4(b));
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
