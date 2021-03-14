using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    class PeriscopeModule : Module<IMyMotorStator> {
        IMyMotorStator first, second;
        IMyCameraBlock camera;
        List<object> menuMethods;
        double range = 20000;
        bool xneg = false;
        public PeriscopeModule() {
            Update = UpdateAction;
            Menu = p => menuMethods;
        }
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
                                    MenuName = "Periscope " + first.CustomName;
                                    if (camera.Orientation.Left == second.Top.Orientation.Up) {
                                        xneg = true;
                                    }


                                    Okay = true;
                                    
                                    menuMethods = new List<object>();
                                    menuMethods.Add(new MenuMethod("Activate", null, Nactivate));
                                    menuMethods.Add(new MenuMethod("Periscope Scan", null, Scan));
                                    menuMethods.Add(new MenuMethod("Camera Module Scan", null, ModuleScan));
                                    menuMethods.Add(new MenuMethod("Planetary Scan", null, PlanetScan));
                                    Active = true;
                                    Nactivate();
                                }
                            }
                        }
                    }
                }
            } else {
                // multiple periscopes
                // new module need some way to make sure new module does not grab control of existing periscope
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
        Menu ModuleScan(MenuModule aMain = null, object argument = null) {
            CameraModule mod;
            GetModule(out mod);
            if (mod != null) {
                MyDetectedEntityInfo entity;
                if (mod.Scan(camera.WorldMatrix.Translation + camera.WorldMatrix.Forward * range, out entity)) {
                    if (entity.Type == MyDetectedEntityType.None) {
                        logger.persist("Module scan empty.");
                    } else {
                        logger.persist(logger.gps(entity.Name, entity.Position));
                        logger.persist("Module scan success.");
                    }
                } else {
                    logger.persist("Module scan failed.");
                }
            }
            return null;
        }
        Menu PlanetScan(MenuModule aMain = null, object argument = null) {
            var orange = range;
            try {
                range = 6000000;
                Scan(aMain, argument);
            } finally {
                range = orange;
            }
            return null;
        }
        Menu Scan(MenuModule aMain = null, object argument = null) {
            if (camera.AvailableScanRange <= range) {
                logger.persist("Camera charging " + camera.TimeUntilScan(range) / 1000 + " seconds remaining.");
            } else {
                MyDetectedEntityInfo entity = camera.Raycast(range);
                if (entity.Type == MyDetectedEntityType.None) {
                    logger.persist("Periscope scanned nothing.");
                } else {
                    if (entity.Type == MyDetectedEntityType.None) {
                        logger.persist("Periscope scan empty.");
                    } else if (entity.EntityId == ModuleManager.Program.Me.CubeGrid.EntityId) {
                        logger.persist("Periscope scan obstructed by grid.");
                    } else {
                        logger.persist(logger.gps(entity.Name, entity.Position));
                        logger.persist("Periscope scan success.");
                        CameraModule mod;
                        GetModule(out mod);
                        if (mod != null) {
                            mod.AddNew(entity);
                        }
                    }
                }
            }
            return null;
        }
        void UpdateAction() {
            if (Active) {
                var sc = controller.Cockpit;
                
                if (sc != null) {
                    var rot = sc.RotationIndicator;
                    logger.log(rot);
                    if (first == null) {
                        logger.log("first null");
                    } else {
                        /*
                         * If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
                         * compute the dot product v * (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
                         */
                        var rad = rot.Y * 0.01f;
                        if (first.WorldMatrix.Up.Dot(camera.WorldMatrix.Up) < 0) {
                            rad = -rad;
                        }
                        first.TargetVelocityRad = rad;
                    }
                    if (second == null) {
                        logger.log("second null");
                    } else {
                        var rad = rot.X * 0.01f;
                        if (xneg) {
                            rad = -rad;
                        }
                        second.TargetVelocityRad = rad;
                    }
                }
            }
        }
    }
}
