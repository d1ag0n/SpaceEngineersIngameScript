using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using System;

namespace IngameScript {
    public class Module<T> : ModuleBase, IModuleBlock {
        readonly HashSet<long> mRegistry = new HashSet<long>();

        public readonly List<T> Blocks;
        
        public IMyCubeGrid Grid => mManager.mProgram.Me.CubeGrid;
        public BoundingSphereD Volume => Grid.WorldVolume;
        public MyOrientedBoundingBoxD OBB => MAF.obb(Grid, Grid.GridSize);
        public MatrixD MyMatrix {
            get {
                var m = Grid.WorldMatrix;
                m.Translation = Volume.Center;
                return m;
            }
        }
        public Module(ModuleManager aManager) : base(aManager) {
            Blocks = new List<T>();
            
        }
        
        public bool GetModule<S>(out S aComponent) where S : class => mManager.GetModule(out aComponent);
        public bool GetModules<S>(List<S> aComponentList) where S : class => mManager.GetModules(aComponentList);
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is T) {
                if (mRegistry.Add(aBlock.EntityId)) {
                    Blocks.Add((T)aBlock);
                    return true;
                }
            }
            return false;
        }
        public override void Remove(IMyTerminalBlock b) {
            if (b is T) {
                Blocks.Remove((T)b);
                mRegistry.Remove(b.EntityId);
            }
        }
    }
}
