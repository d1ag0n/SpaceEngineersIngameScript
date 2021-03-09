using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    class PeriscopeModule : Module<IMyMotorStator> {
        
        IMyMotorStator first, second;
        IMyCameraBlock camera;

        public override bool Accept(IMyTerminalBlock aBlock) {
            bool result = false;
            if (first == null) {
                
                if (aBlock.CustomData.Contains("#periscope")) {
                    result = base.Accept(aBlock);
                    if (result) {
                        first = aBlock as IMyMotorStator;
                        
                        ModuleManager.GetByGrid(first.TopGrid.EntityId, ref second);
                        if (camera != null) {
                            ModuleManager.GetByGrid(second.CubeGrid.EntityId, ref camera);
                        }
                    }
                }
            } else {

            }
            return result;
        }

        public void Update() {
            var rot = controller.RotationIndicator();
            logger.log(rot);
            if (first == null) {
                logger.log("first null");
            } else {
                first.TargetVelocityRad = rot.Y * 0.01f;
            }
            if (second == null) {
                logger.log("second null");
            } else {
                second.TargetVelocityRad = rot.X * 0.01f;
            }
        }
    }
}