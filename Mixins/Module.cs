using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class Module<T> : ModuleBase {
        readonly HashSet<long> mRegistry = new HashSet<long>();
        public readonly List<T> Blocks = new List<T>();
        

        public IMyCubeGrid Grid => ModuleManager.Program.Me.CubeGrid;
        public BoundingSphereD Volume => Grid.WorldVolume;
        public MatrixD MyMatrix {
            get {
                var m = Grid.WorldMatrix;
                m.Translation = Volume.Center;
                return m;
            }
        }
        public Module() {
            ModuleManager.Add(this);
        }
        
        public bool GetModule<S>(out S aComponent) where S : class => ModuleManager.GetModule(out aComponent);
        public bool GetModules<S>(List<S> aComponentList) where S : class => ModuleManager.GetModules(aComponentList);
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is T) {
                if (mRegistry.Add(aBlock.EntityId)) {
                    Blocks.Add((T)aBlock);
                    return true;
                }
            }
            return false;
        }
        public override bool Remove(IMyTerminalBlock b) {
            if (b is T) {
                Blocks.Remove((T)b);
                mRegistry.Remove(b.EntityId);
                return true;
            }
            return false;
        }
    }
}
