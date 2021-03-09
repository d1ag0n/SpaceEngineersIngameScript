using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    class ShipControllerModule : Module<IMyShipController> {
        public readonly bool LargeGrid;
        public readonly float GyroSpeed;
        readonly Logger g;
        public ShipControllerModule() {
            GetModule(out g);
            LargeGrid = ModuleManager.Program.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            GyroSpeed = LargeGrid ? 30 : 60;
        }
        public MyShipVelocities GetShipVelocities() {
            foreach (var sc in Blocks) {
                if (sc.IsFunctional) {
                    return sc.GetShipVelocities();
                }
            }
            return default(MyShipVelocities);
        }
        public MatrixD WorldMatrix {
            get {
                foreach (var sc in Blocks) {
                    return sc.WorldMatrix;
                }
                return default(MatrixD);
            }
        }
    }
}