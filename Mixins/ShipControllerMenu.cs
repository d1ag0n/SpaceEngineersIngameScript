using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ShipControllerMenu : Menu {
        
        readonly ThrustModule mThrust;
        readonly List<MenuItem> mItems = new List<MenuItem>();
        public ShipControllerMenu(MenuModule aMenuModule) : base(aMenuModule) {
            aMenuModule.mManager.GetModule(out mThrust);
            
        }

        public override List<MenuItem> GetPage() {
            mItems.Clear();
            mItems.Add(new MenuItem($"Dampeners {mThrust.Damp}", () => { mThrust.Damp = !mThrust.Damp; return this; }));
            mItems.Add(new MenuItem("Abort All Missions", () => { mController.NewMission(null); return this; }));
            return mItems;
        }

    }
}
