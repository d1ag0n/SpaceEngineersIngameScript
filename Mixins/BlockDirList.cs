using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {

    /// <summary>
    /// Forward = 0,
    /// Backward = 1,
    /// Left = 2,
    /// Right = 3,
    /// Up = 4,
    /// Down = 5
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockDirList<T> where T: IMyTerminalBlock {
        protected readonly List<T>[] mLists = new List<T>[6];

        public BlockDirList() {
            for (int i = 0; i < 6; i++) mLists[i] = new List<T>();
        }
        public void Add(T aBlock, Base6Directions.Direction direction = Base6Directions.Direction.Forward) {
            direction = aBlock.Orientation.TransformDirection(direction);
            aBlock.CustomName = $"{aBlock.GetType().Name} {direction}";
            mLists[(int)direction].Add(aBlock);
        }
     
    }
}
