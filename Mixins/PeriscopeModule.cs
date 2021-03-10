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
                        if (first != null && first.TopGrid != null) {
                            ModuleManager.GetByGrid(first.TopGrid.EntityId, ref second);
                            if (second != null && second.TopGrid != null) {
                                Okay = true;
                                MenuName = "Periscope " + first.CustomName;
                                MenuMethods.Add(new MenuMethod("Activate", Nactivate));
                                MenuMethods.Add(new MenuMethod("Scan", Scan));
                                Active = true;
                                Nactivate();
                            }
                        }
                    }
                }
            } else {

            }
            return result;
        }
        public void Nactivate(object argument = null) {
            if (Okay) {
                first.TargetVelocityRad = 0;
                second.TargetVelocityRad = 0;
                Active = !Active;
                MenuMethods[0].Name = Active ? "Deactivate" : "Activate";
            }
        }
        public void Scan(object argument = null) {
            CameraModule cam;
            if (GetModule(out cam)) {
                var target = camera.WorldMatrix.Translation + camera.WorldMatrix.Forward * 20000;
                MyDetectedEntityInfo entity;
                if (cam.Scan(target, out entity)) {
                    if (entity.Type == MyDetectedEntityType.None) {
                        logger.persist("Periscope scanned nothing.");
                    } else {
                        logger.persist("Periscope scan recorded " + entity.Type);
                    }
                } else {
                    logger.persist("Periscope scan failed.");
                }
            }
            
        }

        public override void Update() {
            if (Active) {
                var rot = controller.RotationIndicator;
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
}