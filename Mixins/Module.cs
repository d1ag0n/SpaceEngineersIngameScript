using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public class Module<T> : ModuleBase, IAccept {
        readonly HashSet<long> mRegistry = new HashSet<long>();
        public readonly List<T> Blocks = new List<T>();
        protected readonly IMyCubeGrid Grid;
        public Module() {
            Grid = ModuleManager.Program.Me.CubeGrid;
            ModuleManager.Add(this);
            Update = Void;
        }
        
        public bool GetModule<S>(out S aComponent) => ModuleManager.GetModule(out aComponent);
        public bool GetModules<S>(List<S> aComponentList) => ModuleManager.GetModules(aComponentList);
        public virtual bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is T) {
                if (mRegistry.Add(aBlock.EntityId)) {
                    Blocks.Add((T)aBlock);
                    return true;
                }
            }
            return false;
        }
        public virtual bool Remove(IMyTerminalBlock b) {
            if (b is T) {
                Blocks.Remove((T)b);
                mRegistry.Remove(b.EntityId);
                return true;
            }
            return false;
        }
    }
}
