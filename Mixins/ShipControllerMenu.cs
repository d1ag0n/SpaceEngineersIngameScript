using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class ShipControllerMenu : Menu {
        readonly ShipControllerModule mController;
        readonly ThrustModule mThrust;
        readonly List<MenuItem> mItems = new List<MenuItem>();
        public ShipControllerMenu(MenuModule aMenuModule, Menu aPrevious = null) : base(aMenuModule, aPrevious) {
            aMenuModule.mManager.GetModule(out mController);
            aMenuModule.mManager.GetModule(out mThrust);
        }

        public override void Update() {
            mItems.Clear();
            mItems.Add(new MenuItem($"Dampeners {mThrust.Damp}", () => { mThrust.Damp = !mThrust.Damp; return null; }));
            mItems.Add(new MenuItem("Abort All Missions", () => { mController.NewMission(null); return null; }));
        }
        public override List<MenuItem> Items() => mItems;

        
    }
}
