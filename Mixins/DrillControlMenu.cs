using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class DrillControlMenu : Menu {
        readonly ATCModule mATC;
        readonly List<MenuItem> mList = new List<MenuItem>();
        readonly List<ATCModule.DrillMission> mDrillMission = new List<ATCModule.DrillMission>();
        public DrillControlMenu(MenuModule aMenuModule) : base(aMenuModule) {
            aMenuModule.mManager.GetModule(out mATC);
            mList.Add(new MenuItem("Recall Drills", recall));
        }
        Menu recall() {
            mATC.RecallAllDrills();
            return this;
        }
        public override List<MenuItem> GetPage() => mList;
    }
}
