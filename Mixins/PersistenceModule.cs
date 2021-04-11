using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using System.Xml.Serialization;

namespace IngameScript {
    public class PersistenceModule : Module<IMyShipController> {
        readonly Dictionary<string, Persistence> mKeys = new Dictionary<string, Persistence>();
        public PersistenceModule(ModuleManager aManager) : base(aManager) {
            onSave = save;
            onLoad = load;
        }
        public void Add(Persistence aPersistence) => mKeys.Add(aPersistence.Key, aPersistence);
        string save() {
            var s = new Serialize();
            var one = false;
            foreach (var p in mKeys) {
                mLog.persist("Saving " + p.Key);
                if (one) {
                    s.mod();
                }
                s.grp(p.Value.Key);
                p.Value.Save(s);
                one = true;
            }
            return s.Clear();
        }
        void load(string aStorage) {
            var s = new Serialize();
            var moduleEntries = new Dictionary<string, List<string>>();
            List<string> work;
            var mods = aStorage.Split(Serialize.MODSEP);
            foreach (var mod in mods) {
                var grps = mod.Split(Serialize.GRPSEP);
                mLog.persist("loaded data key " + grps[0]);
                if (!moduleEntries.TryGetValue(grps[0], out work)) {
                    work = new List<string>();
                    moduleEntries.Add(grps[0], work);
                }
                work.Add(grps[1]);
            }
            foreach (var p in mKeys) {
                if (moduleEntries.TryGetValue(p.Key, out work)) {
                    foreach (var data in work) {
                        mLog.persist("passing data to " + p.Key);
                        p.Value.Load(s, data);
                    }
                }
            }
        }
    }
}
