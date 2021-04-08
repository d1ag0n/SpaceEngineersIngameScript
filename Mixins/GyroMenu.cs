using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class GyroMenu : Menu {
        
        
        readonly GyroModule mGyro;
        readonly List<MenuItem> mItems = new List<MenuItem>();
        GyroAVMission mMission;

        public GyroMenu(MenuModule aModule, Menu aPrevious):base(aModule, aPrevious) {
            aModule.mManager.GetModule(out mGyro);
        }
        public override List<MenuItem> Items() => mItems;
        public override void Update() {
            if (mMission != null) {
                // todo cancel mission
                mMission = null;
            }
            mItems.Clear();
            mItems.Add(MenuItem.CreateItem(mGyro.Active ? "Activate" : "Deactivate", () => { mGyro.Active = !mGyro.Active; }));
            mItems.Add(new MenuItem("Configurator", Configurator));
        }

        Menu Configurator() {
            mMission = new GyroAVMission(mModule.mManager);
            // todo set mission once I decide where missions live
            return new GyroAVMenu(mModule, this);
        }

        /*onPage = p => {
            if (onUpdate != UpdateAction) {
                onUpdate = UpdateAction;
                init();
            }
            mMenuItems.Clear();
            
            return mMenuItems;
        };*/
    }
}