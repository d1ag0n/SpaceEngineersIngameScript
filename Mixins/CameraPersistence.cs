using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using System.Xml.Serialization;
using System;

namespace IngameScript {
    public class CameraPersistence : Persistence {
        readonly CameraModule mCamera;
        public CameraPersistence(CameraModule aCamera) {
            mCamera = aCamera;
            Key = "Camera";
        }

        public override void Load(Serialize s, string data) {
            var ar = data.Split(Serialize.RECSEP);
            foreach (var record in ar) {
                var entry = record.Split(Serialize.UNTSEP);
                if (entry[0] == "Record") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        IEnumerable<string> elements = entries;

                        using (var en = elements.GetEnumerator()) {
                            en.MoveNext();
                            var thy = s.objThyDetectedEntityInfo(en);
                            mCamera.mDetected.Add(thy);
                            mCamera.mLookup.Add(thy.EntityId, thy);
                        }
                    }
                } else if (entry[0] == "Cluster") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        mCamera.mClusterLookup[s.objlong(entries[0])] = s.objlong(entries[1]);
                    }
                } else if (entry[0] == "Ore") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        IEnumerable<string> elements = entries;
                        using (var en = elements.GetEnumerator()) {
                            en.MoveNext();
                            var id = s.objlong(en);
                            var name = s.objstring(en);
                            var pos = s.objVector3D(en);
                            var ba = s.objVector3D(en);
                            var v3l = new Vector3L((long)pos.X, (long)pos.Y, (long)pos.Z);
                            ThyDetectedEntityInfo thy;
                            if (mCamera.mLookup.TryGetValue(id, out thy)) {
                                var o = new Ore(thy, name, v3l);
                                o.BestApproach = ba;
                                thy.AddOre(o);
                            }
                        }
                    }
                }
            }
            if (mCamera.mDetected.Count == 0) {
                mCamera.mLookup.Clear();
                mCamera.mClusterLookup.Clear();
            }
        }

        public override void Save(Serialize s) {
            var one = false;
            foreach (var e in mCamera.mDetected) {
                if (one) {
                    s.rec();
                }
                s.unt("Record");
                s.str(e);
                one = true;
                foreach (var o in e.mOres) {
                    s.rec();
                    s.unt("Ore");
                    s.str(e.EntityId);
                    s.str(o.Name);
                    s.str(o.Location);
                    s.str(o.BestApproach);
                }
            }
            foreach (var p in mCamera.mClusterLookup) {
                if (one) {
                    s.rec();
                }
                s.unt("Cluster");
                s.str(p.Key);
                s.str(p.Value);
                one = true;
            }
        }
    }
}
