using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {

    class BlockDirList<T> where T: IMyCubeBlock {
        protected readonly List<T> mLeft = new List<T>();
        protected readonly List<T> mRight = new List<T>();
        protected readonly List<T> mUp = new List<T>();
        protected readonly List<T> mDown = new List<T>();
        protected readonly List<T> mFront = new List<T>();
        protected readonly List<T> mBack = new List<T>();

        public void Add(IMyShipController aController, T aBlock) {
            var o = aController.Orientation;
            var f = aBlock.Orientation.Forward;

            if (f == o.Forward) {
                mBack.Add(aBlock);
            } else if (f == o.Up) {
                mDown.Add(aBlock);
            } else if (f == o.Left) {
                mRight.Add(aBlock);
            } else if (f == Base6Directions.GetOppositeDirection(o.Forward)) {
                mFront.Add(aBlock);
            } else if (f == Base6Directions.GetOppositeDirection(o.Up)) {
                mUp.Add(aBlock);
            } else if (f == Base6Directions.GetOppositeDirection(o.Left)) {
                mLeft.Add(aBlock);
            }
        }
        
    }
}
