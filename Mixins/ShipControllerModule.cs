using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    class ShipControllerModule : Module<IMyShipController> {

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