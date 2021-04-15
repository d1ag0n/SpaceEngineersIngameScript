using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class CameraMenu : Menu {
        readonly NameGen namer = new NameGen();
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        readonly CameraModule mCamera;
        readonly HashSet<string> names = new HashSet<string>();
        public CameraMenu(MenuModule aMenuModule) : base(aMenuModule) {
            Title = "Camera Records";
            aMenuModule.mManager.GetModule(out mCamera);
        }
        public string GenerateName() {
            string result;
            do {
                result = namer.Next(MAF.random.Next(2, 4));
            } while (names.Contains(result));
            names.Add(result);
            return result;
        }
        public override List<MenuItem> GetPage() {
            var detected = mCamera.mDetected;
            if (mPage < 0) {
                mCamera.Sort();
            }
            var pageIndex = PageIndex(mPage, detected.Count);

            //mLog.persist($"detected.count={detected.Count}, pageIndex={pageIndex}");
            mMenuItems.Clear();
            int count = 0;
            while (count < 6 && pageIndex < detected.Count) {
                var thy = detected[pageIndex];
                var name = thy.Name;
                if (name == "Asteroid") {
                    name = thy.Name = GenerateName();
                }
                if (thy.Type == ThyDetectedEntityType.Asteroid) {
                    name = $"{thy.Name} Asteroid";
                } else if (thy.Type == ThyDetectedEntityType.AsteroidCluster) {
                    name = $"{thy.Name} Cluster";
                }
                mMenuItems.Add(new MenuItem(name, EntityMenuGen(thy)));
                pageIndex++;
                count++;
            }
            return mMenuItems;
        }

        Func<Menu> EntityMenuGen(ThyDetectedEntityInfo aEntity) => () => new ThyDetectedEntityMenu(mMenuModule, aEntity);
    }
}

        