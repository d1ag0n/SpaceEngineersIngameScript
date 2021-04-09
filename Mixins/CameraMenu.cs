using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class CameraMenu : Menu {
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        readonly CameraModule mCamera;
        public CameraMenu(MenuModule aMenuModule) : base(aMenuModule) {
            aMenuModule.mManager.GetModule(out mCamera);
        }
        public override List<MenuItem> GetPage() {
            var detected = mCamera.mDetected;
            var pageIndex = PageIndex(mPage, detected.Count);
            mMenuItems.Clear();
            while (pageIndex < 6 && pageIndex < detected.Count) {
                var thy = detected[pageIndex];
                var name = thy.Name;
                if (thy.Type == ThyDetectedEntityType.Asteroid) {
                    name = $"{thy.Name} Asteroid";
                } else if (thy.Type == ThyDetectedEntityType.AsteroidCluster) {
                    name = $"{thy.Name} Cluster";
                }
                mMenuItems.Add(new MenuItem(name, DetectedEntityMenu));
            }
            return mMenuItems;
        }

        Menu DetectedEntityMenu() {
            // todo 
            return null;
        }
    }
}

        