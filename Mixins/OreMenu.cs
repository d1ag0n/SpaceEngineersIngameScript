using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class OreMenu : Menu {
        readonly Ore mOre;

        public OreMenu(MenuModule aMenuModule, Ore aOre) : base(aMenuModule) {
            mOre = aOre;
            mItems.Add(new MenuItem("Send Drill Drone", sendDrillDrone));
            mItems.Add(new MenuItem(mLog.gps(mOre.Name,mOre.Location), this));
            
        }
        public override List<MenuItem> GetPage() => mItems;
        Menu sendDrillDrone() {
            ATCModule atc;
            if (mManager.GetModule(out atc)) {
                if (atc.SendDrill(mOre)) {
                    mLog.persist(mLog.gps(mOre.Name, mOre.Location));
                } else {
                    mLog.persist("Drill not sent.");
                }
            }
            return null;
        }
    }
}
