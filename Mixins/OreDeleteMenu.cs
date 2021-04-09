using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class OreDeleteMenu : Menu {
        ThyDetectedEntityInfo mEntity;
        public OreDeleteMenu(MenuModule aMenuModule, ThyDetectedEntityInfo aEntity) : base(aMenuModule) {
            mEntity = aEntity;
            Title = "Confirm Deletion";
            mItems.Add(new MenuItem("Yes", delete));
        }
        public override List<MenuItem> GetPage() => mItems;
        Menu delete() {
            CameraModule cam;
            if (mManager.GetModule(out cam)) {
                cam.DeleteRecord(mEntity);
            }
            return null;
        }
    }
}
