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
        List<object> menuMethods;
        public override List<object> MenuMethods(int aPage) => menuMethods;

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
                                ModuleManager.GetByGrid(second.TopGrid.EntityId, ref camera);
                                if (camera != null) {
                                    camera.CustomName = $"!Periscope {first.CustomName} - Camera";
                                }
                                Okay = true;
                                MenuName = "Periscope " + first.CustomName;
                                menuMethods = new List<object>();
                                menuMethods.Add(new MenuMethod("Activate", null, Nactivate));
                                menuMethods.Add(new MenuMethod("Scan", null, Scan));
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
        public Menu Nactivate(MenuModule aMain = null, object argument = null) {
            if (Okay) {
                first.TargetVelocityRad = 0;
                second.TargetVelocityRad = 0;
                Active = !Active;
                ((MenuMethod)menuMethods[0]).Name = Active ? "Deactivate" : "Activate";
                if (Active) {
                    camera.CustomName = "!" + camera.CustomName;
                    logger.persist($"View {camera.CustomName}");
                } else {
                    camera.CustomName = camera.CustomName.Substring(1);
                }
            }
            return null;
        }
        Menu Scan(MenuModule aMain = null, object argument = null) {
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
            } else {
                logger.persist("Periscope scan requires CameraModule");
            }
            return null;
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