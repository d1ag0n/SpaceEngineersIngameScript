using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class LogModule : Module<IMyTextPanel> {
        int pcount = 0;
        string nl => Environment.NewLine;
        readonly StringBuilder mSBWork = new StringBuilder();
        readonly StringBuilder mSBLog = new StringBuilder();
        readonly List<string> mPersistent = new List<string>(25);

        public LogModule(ModuleManager aManager) : base(aManager) {
            onUpdate = UpdateAction;
        }
        public void log(MyDetectedEntityInfo e) => log(string4(e));
        public string string4(BoundingBoxD b) {
            mSBWork.Clear();
            mSBWork.AppendLine(b.Min.ToString());
            mSBWork.AppendLine(b.Center.ToString());
            mSBWork.AppendLine(b.Max.ToString());
            return mSBWork.ToString();
        }
        public string string4(MyDetectedEntityInfo e) {

            mSBWork.Clear();
            mSBWork.AppendLine($"{e.Name} {e.Type} {e.EntityId}");

            //mWork.AppendLine(e.EntityId.ToString());


            //mWork.AppendLine(gps("BoxMin", e.BoundingBox.Min));
            //mWork.AppendLine(gps("BoxCenter", e.BoundingBox.Center));
            //mWork.AppendLine(gps("BoxMax", e.BoundingBox.Max));
            var bb = e.BoundingBox;
            
            foreach (var c in bb.GetCorners()) {
                //mWork.AppendLine(gps("corner" + i++, c));
            }
            //mWork.AppendLine("Translation");
            //mWork.AppendLine(string4(e.Orientation.Translation));
            //mWork.AppendLine("Up");
            //mWork.AppendLine(string4(e.Orientation.Up));
            if (e.HitPosition.HasValue) {
                //mWork.AppendLine("hit");
                mSBWork.AppendLine(gps("hit", e.HitPosition.Value));
            }
            return mSBWork.ToString();
        }

        public override bool Accept(IMyTerminalBlock b) {
            var result = false;
            if (b.CustomData.Contains("#logconsole")) {
                result = base.Accept(b);
                if (result) {
                    var p = b as IMyTextPanel;
                    p.ContentType = ContentType.TEXT_AND_IMAGE;
                    p.Font = "Monospace";
                    p.CustomName = "Log Console - " + Blocks.Count;
                    if (p.FontColor == Color.White) {
                        p.FontColor = new Color(255, 176, 0);
                    }
                }
            }
            
            return result;
        }

        public void log(Vector3D v) => log(string4(v));
        public string string4(Vector3D v) => $"X {v.X:f2}{nl}Y {v.Y:f2}{nl}Z {v.Z:f2}";
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
                        mSBLog.AppendLine();
                    } else if (arg is Vector3D) {
                        mSBLog.AppendLine();
                        log((Vector3D)arg);
                    } else if (arg is double) {
                        mSBLog.Append(((double)arg).ToString("F3"));
                    } else {
                        mSBLog.Append(arg.ToString());
                    }
                }
            }
            mSBLog.AppendLine();
        }
        string clear() {
            string result = get();
            mSBLog.Clear();
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
            var c = mPersistent.Count;
            if (c > 15) {
                mPersistent.RemoveAt(0);
            } else if (c == 0) {
                pcount = 0;
            }
            mPersistent.Add(aMessage);
        }
        public string get() {
            for (int i = 0; i < mPersistent.Count; i++) {
                mSBLog.Insert(0, Environment.NewLine);
                mSBLog.Insert(0, mPersistent[i]);
            }
            return mSBLog.ToString();
        }
        
        void UpdateAction() {
            pcount++;
            if (pcount == 90) {
                pcount = 0;
                if (mPersistent.Count > 0) {
                    mPersistent.RemoveAt(0);
                }
            }
            var str = clear();
            foreach (var tp in Blocks) {
                tp.WriteText(str);
            }
            mManager.mProgram.Echo(str);
        }
    }
}
