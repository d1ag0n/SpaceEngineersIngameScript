using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    public class ShipControllerModule : Module<IMyShipController> {
        public readonly bool LargeGrid;
        public readonly float GyroSpeed;
        
        public ShipControllerModule() {
            LargeGrid = ModuleManager.Program.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            GyroSpeed = LargeGrid ? 30 : 60;
        }
        public Vector2 RotationIndicator() {
            foreach (var sc in Blocks) {
                if (sc.IsFunctional && sc.IsUnderControl) {
                    return sc.RotationIndicator;
                }
            }
            return default(Vector2);
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
                    if (sc.IsMainCockpit) {
                        return sc.WorldMatrix;
                    }
                }
                return default(MatrixD);
            }
        }
    }
}