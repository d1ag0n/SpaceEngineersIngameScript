using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {

    class BlockDirList<T> where T: IMyTerminalBlock {
        protected readonly List<T> mLeft = new List<T>();
        protected readonly List<T> mRight = new List<T>();
        protected readonly List<T> mUp = new List<T>();
        protected readonly List<T> mDown = new List<T>();
        protected readonly List<T> mFront = new List<T>();
        protected readonly List<T> mBack = new List<T>();

        public void Add(IMyShipController aController, T aBlock, string aPrefix = "") {
            var o = aController.Orientation;
            var f = aBlock.Orientation.Forward;
            
            if (aPrefix == "") {
                aPrefix = typeof(T).ToString();
            }
            aBlock.ShowInTerminal = false;
            
            if (f == o.Forward) {
                mBack.Add(aBlock);
                aBlock.CustomName = $"{aPrefix}Back";
            } else if (f == o.Up) {
                mDown.Add(aBlock);
                aBlock.CustomName = $"{aPrefix}Down";
            } else if (f == o.Left) {
                mRight.Add(aBlock);
                aBlock.CustomName = $"{aPrefix}Right";
            } else if (f == Base6Directions.GetOppositeDirection(o.Forward)) {
                mFront.Add(aBlock);
                aBlock.CustomName = $"{aPrefix}Front";
            } else if (f == Base6Directions.GetOppositeDirection(o.Up)) {
                mUp.Add(aBlock);
                aBlock.CustomName = $"{aPrefix}Up";
            } else if (f == Base6Directions.GetOppositeDirection(o.Left)) {
                mLeft.Add(aBlock);
                aBlock.CustomName = $"{aPrefix}Left";
            }
        }
        
    }
}
