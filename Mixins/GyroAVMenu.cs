using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class GyroAVMenu : Menu {
        const double min = 0.001;

        readonly GyroModule mGyro;

        // todo make properties that point to GyroModule
        double difMax = 0.09;   // angular velocity difference threshold
        double slowFact = 20.0; // slow down factor how quickly the ship tries to slow down toward the end of a turn
        double fastFact = 2.0;  // speed up factor
        double smallMax = 0.4;  // angle remaining in turn when smallFactor is applied
        double smallFact = 1.9; // factor applied when within smallMax



        public GyroAVMenu(MenuModule aMenuModule, Menu aPrevious) : base(aMenuModule) {
            mManager.GetModule(out mGyro);
            mItems.Add(MenuItem.CreateItem(strRaiseDifMax, raiseDifMax, this));
            mItems.Add(MenuItem.CreateItem(strLowerDifMax, lowerDifMax, this));
            mItems.Add(MenuItem.CreateItem(strRaiseFastFact, raiseFastFact, this));
            mItems.Add(MenuItem.CreateItem(strLowerFastFact, lowerFastFact, this));
            mItems.Add(MenuItem.CreateItem(strRaiseSlowFact, raiseSlowFact, this));
            mItems.Add(MenuItem.CreateItem(strLowerSlowFact, lowerSlowFact, this));
            mItems.Add(MenuItem.CreateItem(strRaiseSmallMax, raiseSmallMax, this));
            mItems.Add(MenuItem.CreateItem(strLowerSmallMax, lowerSmallMax, this));
            mItems.Add(MenuItem.CreateItem(strRaiseSmallFact, raiseSmallFact, this));
            mItems.Add(MenuItem.CreateItem(strLowerSmallFact, lowerSmallFact, this));
            mItems.Add(MenuItem.CreateItem("Set Default Values", defaults, this));
        }
        public override List<MenuItem> GetPage() => mItems;
        
        void defaults() {
            difMax = 0.09;
            slowFact = 20.0;
            fastFact = 2.0;
            smallMax = 0.4;
            smallFact = 1.9;
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
    }
}
