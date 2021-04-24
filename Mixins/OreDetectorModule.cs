using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {

    public class OreDetectorModule : Module<IMyOreDetector> {
        public OreDetectorModule(ModuleManager aManager):base(aManager) { }
        public int Scan(ThyDetectedEntityInfo thy, Vector3D aPos, out MyDetectedEntityInfo info, bool update) {
            int result = 0;
            info = default(MyDetectedEntityInfo);
            foreach (var detector in Blocks) {
                var range = detector.GetValue<double>("AvailableScanRange");
                range *= range;
                var disp = detector.WorldMatrix.Translation - aPos;
                var dist = disp.LengthSquared();
                if (dist >= range) {
                    continue;
                }

                detector.SetValue("RaycastTarget", aPos);
                info = detector.GetValue<MyDetectedEntityInfo>(update ? "DirectResult" : "RaycastResult");

                if (info.TimeStamp != 0) {
                    result = 1;
                    if (info.Name != "") {
                        result = 2;
                        if (thy.AddOre(info)) {
                            result = 3;
                            mLog.persist($"New {info.Name} Deposit found!");
                        }
                    }
                    break;
                } else {
                    mLog.persist("ORE Timestamp Zero");
                }
                //detector.SetValue("ScanEpoch", 0L);
                //throw new Exception("shouldnt be able to write ScanEpoch");
            }
            return result;
        }
        public static void UpdateScan(ModuleManager aManager, ThyDetectedEntityInfo aEntity) =>
            aManager.mMachines.Enqueue(_UpdateScan(aManager, aEntity));
        static IEnumerator<bool> _UpdateScan(ModuleManager aManager, ThyDetectedEntityInfo aEntity) {
            
            CameraModule cam;
            OreDetectorModule detector;
            MyDetectedEntityInfo info;
            var entity = new MyDetectedEntityInfo();
            ThyDetectedEntityInfo thy;
            int index = aEntity.mOres.Count - 1;
            aManager.GetModule(out cam);
            aManager.GetModule(out detector);

            for (; index > -1; index--) {
                var ore = aEntity.mOres[index];
                if (cam.Scan(ref ore.Location, ref entity, out thy)) {
                    if (entity.HitPosition.HasValue) {
                        var hit = entity.HitPosition.Value;
                        // todo DP of approach to make sure it's on the correct side
                        if (ore.BestApproach.IsZero()) {
                            ore.BestApproach = hit;
                        } else {
                            var curApp = (ore.BestApproach - ore.Location).LengthSquared();
                            var newApp = (ore.Location - hit).LengthSquared();
                            if (newApp < curApp) {
                                ore.BestApproach = hit;
                            }
                        }
                        aEntity.mOres[index] = ore;
                    }
                    var scanResult = detector.Scan(aEntity, ore.Location, out info, true);
                    if (scanResult == 1) {
                        aEntity.mOres.RemoveAtFast(index);
                        aEntity.SortOre();
                        aManager.mLog.persist(aManager.mLog.gps("removed", ore.Location));
                        index++;
                    }
                } else {
                    aManager.mLog.persist(aManager.mLog.gps("ExpectedSuccess", ore.Location));
                }

                
                yield return true;
            }
            
        }
    }
}
