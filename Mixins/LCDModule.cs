using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    class LCDModule : Module<IMyTextPanel> {
        readonly List<IMyTextPanel> mLCDs = new List<IMyTextPanel>();
        


        public override bool Remove(IMyCubeBlock b) {
            var result = base.Remove(b);
            if (result) {
                mLCDs.Remove(b as IMyTextPanel);
            }
            return result;
        }

        public void WriteAll(string str) {
            foreach (var p in mLCDs) {
                p.WriteText(str);
            }
        }
    }
}
