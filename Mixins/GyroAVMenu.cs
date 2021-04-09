using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public class GyroAVMenu : Menu {
        const double min = 0.001;

        readonly List<MenuItem> mItems = new List<MenuItem>();

        readonly GyroModule mGyro;

        // todo make properties that point to GyroModule
        double difMax = 0.09;   // angular velocity difference threshold
        double slowFact = 20.0; // slow down factor how quickly the ship tries to slow down toward the end of a turn
        double fastFact = 2.0;  // speed up factor
        double smallMax = 0.4;  // angle remaining in turn when smallFactor is applied
        double smallFact = 1.9; // factor applied when within smallMax



        public GyroAVMenu(MenuModule aModule, Menu aPrevious) : base(aModule, aPrevious) {
            aModule.mManager.GetModule(out mGyro);
            mItems.Add(MenuItem.CreateItem(strRaiseDifMax, raiseDifMax));
            mItems.Add(MenuItem.CreateItem(strLowerDifMax, lowerDifMax));
            mItems.Add(MenuItem.CreateItem(strRaiseFastFact, raiseFastFact));
            mItems.Add(MenuItem.CreateItem(strLowerFastFact, lowerFastFact));
            mItems.Add(MenuItem.CreateItem(strRaiseSlowFact, raiseSlowFact));
            mItems.Add(MenuItem.CreateItem(strLowerSlowFact, lowerSlowFact));
            mItems.Add(MenuItem.CreateItem(strRaiseSmallMax, raiseSmallMax));
            mItems.Add(MenuItem.CreateItem(strLowerSmallMax, lowerSmallMax));
            mItems.Add(MenuItem.CreateItem(strRaiseSmallFact, raiseSmallFact));
            mItems.Add(MenuItem.CreateItem(strLowerSmallFact, lowerSmallFact));
            mItems.Add(MenuItem.CreateItem("Set Default Values", defaults));
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
