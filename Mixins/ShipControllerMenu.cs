using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ShipControllerMenu : Menu {
        
        readonly ThrustModule mThrust;
        readonly List<MenuItem> mItems = new List<MenuItem>();
        public ShipControllerMenu(MenuModule aMenuModule) : base(aMenuModule) {
            aMenuModule.mManager.GetModule(out mThrust);
            mItems.Add(new MenuItem($"Dampeners {mThrust.Damp}", () => { mThrust.Damp = !mThrust.Damp; return null; }));
            mItems.Add(new MenuItem("Abort All Missions", () => { mController.NewMission(null); return null; }));
        }

        public override List<MenuItem> GetPage() => mItems;

    }
}
