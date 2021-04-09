using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class GyroMenu : Menu {
        
        
        readonly GyroModule mGyro;
        
        GyroAVMission mMission;

        public GyroMenu(MenuModule aMenuModule, Menu aPrevious):base(aMenuModule) {
            aMenuModule.mManager.GetModule(out mGyro);
            mItems.Add(MenuItem.CreateItem(mGyro.Active ? "Activate" : "Deactivate", () => { mGyro.Active = !mGyro.Active; }));
            mItems.Add(new MenuItem("Configurator", Configurator));
        }
        public override List<MenuItem> GetPage() => mItems;

        Menu Configurator() {
            mMission = new GyroAVMission(mMenuModule.mManager);
            // todo set mission once I decide where missions live
            return new GyroAVMenu(mMenuModule, this);
        }
    }
}