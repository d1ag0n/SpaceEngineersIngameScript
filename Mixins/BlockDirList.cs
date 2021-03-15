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

        public void Add(IMyShipController aController, T aThrust) {
            if (aController == null) {
                throw new Exception("Controller null!");
            }
            if (aThrust == null) {
                throw new Exception("Thrust null!");
            }
            var o = aController.Orientation;
            var f = aThrust.Orientation.Forward;

            if (f == o.Forward) {
                mBack.Add(aThrust);
            } else if (f == o.Up) {
                mDown.Add(aThrust);
            } else if (f == o.Left) {
                mRight.Add(aThrust);
            } else if (f == Base6Directions.GetOppositeDirection(o.Forward)) {
                mFront.Add(aThrust);
            } else if (f == Base6Directions.GetOppositeDirection(o.Up)) {
                mUp.Add(aThrust);
            } else if (f == Base6Directions.GetOppositeDirection(o.Left)) {
                mLeft.Add(aThrust);
            }
        }
        
    }
}
