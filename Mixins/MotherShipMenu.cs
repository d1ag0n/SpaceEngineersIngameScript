using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class MotherShipMenu : Menu {
        readonly PeriscopeMenu mPeriscope;
        readonly CameraMenu mCamera;
        public MotherShipMenu(MenuModule aMenuModule) : base(aMenuModule) {
            Title = "Ship Control";
            mPeriscope = new PeriscopeMenu(aMenuModule);
            mCamera = new CameraMenu(aMenuModule);
            mItems.Add(new MenuItem("Periscope", () => mPeriscope));
            mItems.Add(new MenuItem("Camera", () => mCamera));
        }
        public override List<MenuItem> GetPage() => mItems;
    }
}
