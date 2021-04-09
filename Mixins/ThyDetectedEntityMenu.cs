using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class ThyDetectedEntityMenu : Menu {
        readonly ThyDetectedEntityInfo mEntity;
        public ThyDetectedEntityMenu(MenuModule aMenuModule, ThyDetectedEntityInfo aEntity):base(aMenuModule) {
            mEntity = aEntity;
            
        }
        public override List<MenuItem> GetPage() {
            var page = mPage % 2;
            Title = $"Camera Record for {mEntity.Name} {mEntity.EntityId}";
            mItems.Clear();
            if (page == 0) {
                var ts = (DateTime.Now - mEntity.TimeStamp).TotalHours;
                mItems.Add(new MenuItem($"Time: {mEntity.TimeStamp} ({ts:f2} hours ago)"));
                mItems.Add(new MenuItem($"Relationship: {mEntity.Relationship} - Type: {mEntity.Type}"));
                mItems.Add(new MenuItem(mLog.gps($"{mEntity.Name}", mEntity.Position)));
                mItems.Add(new MenuItem($"Distance: {(mEntity.Position - mController.MyMatrix.Translation).Length():f0} - Radius: {mEntity.WorldVolume.Radius}"));
                mItems.Add(new MenuItem("Designate Target", designateTarget));
                mItems.Add(new MenuItem("Ores: " + string.Join(", ", mEntity.mOreTypes), () => new OreListMenu(mMenuModule, mEntity)));
            } else {
                mItems.Add(new MenuItem("Delete", () => new OreDeleteMenu(mMenuModule, mEntity)));
            }
            return mItems;
        }

        Menu designateTarget() {
            mController.NewMission(new OrbitMission(mManager, mEntity));
            return this;
        }
    }
}
