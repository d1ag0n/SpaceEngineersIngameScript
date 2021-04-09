using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class OreListMenu : Menu {
        readonly ThyDetectedEntityInfo mEntity;
        readonly List<MenuItem> mItems = new List<MenuItem>();
        public OreMenu(MenuModule aMenuModule, ThyDetectedEntityInfo aEntity) : base(aMenuModule) {
            mEntity = aEntity;
        }

        public override List<MenuItem> GetPage() {
            var pageIndex = PageIndex(mPage, mEntity.mOres.Count);
            mItems.Clear();
            int count = 0;
            while (pageIndex < mEntity.mOres.Count && count < 6) {
                var ore = mEntity.mOres[pageIndex];
                mItems.Add(new MenuItem($"{o.Name} - Altitude: {(mEntity.Position - ore.Location).Length()}", o, (m, state) => {
                }
                pageIndex++;
                count++;
            }
        }

        Func<Menu> OreMenuGen(Ore aOre) => () => new OreMenu(aOre);
    }
}
