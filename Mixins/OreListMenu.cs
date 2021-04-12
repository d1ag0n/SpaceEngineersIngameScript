using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class OreListMenu : Menu {
        readonly ThyDetectedEntityInfo mEntity;
        
        public OreListMenu(MenuModule aMenuModule, ThyDetectedEntityInfo aEntity) : base(aMenuModule) {
            mEntity = aEntity;
        }

        public override List<MenuItem> GetPage() {
            var pageIndex = PageIndex(mPage, mEntity.mOres.Count);
            mLog.persist($"pageIndex={pageIndex}, mPage={mPage}, ores={mEntity.mOres.Count}");
            mItems.Clear();
            int count = 0;
            while (pageIndex < mEntity.mOres.Count && count < 6) {
                var ore = mEntity.mOres[pageIndex];
                mItems.Add(new MenuItem($"{ore.Name} - Altitude: {(mEntity.Position - ore.Location).Length()}", OreMenuGen(ore)));
                pageIndex++;
                count++;
            }
            return mItems;
        }

        Func<Menu> OreMenuGen(Ore aOre) => () => new OreMenu(mMenuModule, aOre);
    }
}
