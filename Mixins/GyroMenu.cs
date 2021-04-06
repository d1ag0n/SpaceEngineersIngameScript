using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    class GyroMenu {
        const double min = 0.001;
        // todo make properties that point to GyroModule
        double difMax = 0.09;   // angular velocity difference threshold
        double slowFact = 20.0; // slow down factor how quickly the ship tries to slow down toward the end of a turn
        double fastFact = 2.0;  // speed up factor
        double smallMax = 0.4;  // angle remaining in turn when smallFactor is applied
        double smallFact = 1.9; // factor applied when within smallMax


        Vector3D configDir;
        int configCount = 0;


        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        List<MenuItem> configMenu(int page) {
            mMenuItems.Clear();
            if (page == 0) {
                mMenuItems.Add(new MenuItem(strRaiseDifMax, raiseDifMax));
                mMenuItems.Add(new MenuItem(strLowerDifMax, lowerDifMax));
                mMenuItems.Add(new MenuItem(strRaiseFastFact, raiseFastFact));
                mMenuItems.Add(new MenuItem(strLowerFastFact, lowerFastFact));
                mMenuItems.Add(new MenuItem(strRaiseSlowFact, raiseSlowFact));
                mMenuItems.Add(new MenuItem(strLowerSlowFact, lowerSlowFact));
            } else if (page == 1) {
                mMenuItems.Add(new MenuItem(strRaiseSmallMax, raiseSmallMax));
                mMenuItems.Add(new MenuItem(strLowerSmallMax, lowerSmallMax));
                mMenuItems.Add(new MenuItem(strRaiseSmallFact, raiseSmallFact));
                mMenuItems.Add(new MenuItem(strLowerSmallFact, lowerSmallFact));
                mMenuItems.Add(new MenuItem("Set Default Values", () => {
                    difMax = 0.09;
                    slowFact = 20.0;
                    fastFact = 2.0;
                    smallMax = 0.4;
                    smallFact = 1.9;
                }));
            }
            return mMenuItems;

        }
        string strRaiseDifMax => $"Increase max difference for AV correction: {difMax.ToString("f4")}";
        void raiseDifMax() => difMax *= 1.1;
        string strLowerDifMax => $"Decrease max difference for AV correction: {difMax.ToString("f4")}";
        void lowerDifMax() {
            difMax *= 0.9;
            if (difMax < min) {
                difMax = min;
            }
        }

        string strRaiseFastFact => $"Increase AV acceleration factor:           {fastFact.ToString("f4")}";
        void raiseFastFact() => fastFact *= 1.1;
        string strLowerFastFact => $"Decrease AV acceleration factor:           {fastFact.ToString("f4")}";
        void lowerFastFact() {
            fastFact *= 0.9;
            if (fastFact < min) {
                fastFact = min;
            }
        }

        string strRaiseSlowFact => $"Increase AV deceleration factor:           {slowFact.ToString("f4")}";
        void raiseSlowFact() => slowFact *= 1.1;
        string strLowerSlowFact => $"Decrease AV deceleration factor:           {slowFact.ToString("f4")}";
        void lowerSlowFact() {
            slowFact *= 0.9;
            if (slowFact < min) {
                slowFact = min;
            }
        }

        string strRaiseSmallMax => $"Increase small turn size:                {smallMax.ToString("f4")}";
        void raiseSmallMax() => smallMax *= 1.1;
        string strLowerSmallMax => $"Decrease small turn size:                {smallMax.ToString("f4")}";
        void lowerSmallMax() {
            smallMax *= 0.9;
            if (smallMax < min) {
                smallMax = min;
            }
        }
        string strRaiseSmallFact => $"Increase factor applied to small turns:  {smallFact.ToString("f4")}";
        void raiseSmallFact() => smallFact *= 1.1;
        string strLowerSmallFact => $"Decrease factor applied to small turns:  {smallFact.ToString("f4")}";
        void lowerSmallFact() {
            smallFact *= 0.9;
            if (smallFact < min) {
                smallFact = min;
            }
        }
        Menu ConfigAction(MenuModule aMain, object argument) {
    configCount = 0;
    configDir = Vector3D.Up;
    Active = true;
    init();
    onUpdate = () => {
        logger.log("config update");
        if (controller.ShipVelocities.AngularVelocity.LengthSquared() < 1 && MAF.angleBetween(controller.Remote.WorldMatrix.Forward, configDir) < 0.01) {
            logger.log("config waiting");
            init();
            configCount++;
            if (configCount == 18) {
                configDir = -configDir;
            }
        } else {
            logger.log("config turning");
            configCount = 0;
            SetTargetDirection(configDir);
            UpdateAction();
        }
    };
    return new Menu(aMain, "Angular Velocity Configurator", configMenu);
}
void Nactivate() {
    Active = !Active;
    init();
    var emm = mMenuItems[0];
    mMenuItems[0] = new MenuItem(Active ? "Deactivate" : "Activate", emm.State, emm.Method);
}
onPage = p => {
    if (onUpdate != UpdateAction) {
        onUpdate = UpdateAction;
        init();
    }
    mMenuItems.Clear();
    mMenuItems.Add(new MenuItem(Active ? "Activate" : "Deactivate", Nactivate));
    mMenuItems.Add(new MenuItem("Configurator", null, ConfigAction));
    return mMenuItems;
};
}
}