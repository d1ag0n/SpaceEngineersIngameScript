using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    class ShipControllerMenu {

        onPage = p => {
    mMenuMethods.Clear();


    //mMenuMethods.Add(new MenuItem("Random Mission", () => Mission = new RandomMission(this, new BoundingSphereD(Remote.CenterOfMass + MAF.ranDir() * 1100.0, 0))));

    mMenuMethods.Add(new MenuItem($"Dampeners {Thrust.Damp}", () => {
            Thrust.Damp = !Thrust.Damp;
        }));

    mMenuMethods.Add(new MenuItem("Abort All Missions", () => {
            mMissionStack.Clear();
            mMission = null;
            Thrust.Damp = true;
        }));

    return mMenuMethods;
};
}