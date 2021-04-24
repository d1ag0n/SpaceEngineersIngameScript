using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ShipControllerMenu : Menu {
        
        readonly ThrustModule mThrust;
        readonly GyroModule mGyro;
        
        public ShipControllerMenu(MenuModule aMenuModule) : base(aMenuModule) {
            aMenuModule.mManager.GetModule(out mThrust);
            aMenuModule.mManager.GetModule(out mGyro);
        }

        public override List<MenuItem> GetPage() {
            mItems.Clear();
            mItems.Add(new MenuItem($"Manual Control {mController.mManual}", manual));
            mItems.Add(new MenuItem("Abort All Missions", abort));
            return mItems;
        }
        Menu abort() {
            mController.AbortAllMissions();
            mController.mManual = true;
            return this;
        }
        Menu manual() {
            if (mController.OnMission) {
                mLog.persist("Cannot set manual control while on a mission.");
            } else {
                mController.mManual = !mController.mManual;
            }
            return this;
        }
    }
}
