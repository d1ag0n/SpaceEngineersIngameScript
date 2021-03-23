using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    class PeriscopeModule : Module<IMyMotorStator> {
        IMyMotorStator first, second;
        IMyCameraBlock camera;
        readonly List<MenuItem> mMenuMethods = new List<MenuItem>();
        double range = 20000;
        bool xneg = false;
        public PeriscopeModule() {
            onUpdate = UpdateAction;
            onPage = p => {
                if (Okay) {
                    mMenuMethods.Clear();
                    mMenuMethods.Add(new MenuItem(Active ? "Deactivate" : "Activate", Nactivate));
                    //mMenuMethods.Add(new MenuItem($"Periscope Scan {camera.AvailableScanRange:f0}m Available", null, Scan));
                    mMenuMethods.Add(new MenuItem("Fly Out", () => {
                        var pos = camera.WorldMatrix.Translation + camera.WorldMatrix.Forward * range;
                        controller.Target = new ThyDetectedEntityInfo(pos);
                    }));
                    mMenuMethods.Add(new MenuItem("Camera Module Scan", null, ModuleScan));
                    mMenuMethods.Add(new MenuItem("Planetary Scan", null, PlanetScan));
                    mMenuMethods.Add(new MenuItem($"Increase Range {range:f0}", () => {
                        range += 10000;
                    }));
                    mMenuMethods.Add(new MenuItem($"Decrease Range {range:f0}", () => {
                        if (range > 10000) range -= 10000;
                    }));
                }
                return mMenuMethods;
            };
            onSave = SaveDel;
            onLoad = LoadDel;
        }
        void LoadDel(Serialize s, string aData) {
            var ar = aData.Split(Serialize.RECSEP);
            foreach (var record in ar) {
                var entry = record.Split(Serialize.UNTSEP);
                switch (entry[0]) {
                    case "range":
                        double.TryParse(entry[1], out range);
                        break;
                }
            }
        }
        void SaveDel(Serialize s) {
            s.unt("range");
            s.str(range);
        }

        public override bool Accept(IMyTerminalBlock b) {
            bool result = false;

            if (first == null) {

                if (b.CustomData.Contains("#periscope")) {
                    result = base.Accept(b);
                    if (result) {
                        first = b as IMyMotorStator;
                        first.ShowInTerminal = false;
                        if (first != null && first.TopGrid != null) {
                            ModuleManager.GetByGrid(first.TopGrid.EntityId, ref second);
                            if (second != null && second.TopGrid != null) {
                                second.ShowInTerminal = false;
                                ModuleManager.GetByGrid(second.TopGrid.EntityId, ref camera);
                                if (camera != null) {
                                    camera.CustomName = $"!Periscope {first.CustomName} - Camera";
                                    camera.Enabled =
                                    camera.EnableRaycast = true;
                                    MenuName = "Periscope " + first.CustomName;
                                    if (camera.Orientation.Left == second.Top.Orientation.Up) {
                                        xneg = true;
                                    }
                                    Okay = true;

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
        void Nactivate() {
            if (Okay) {
                first.TargetVelocityRad = 0;
                second.TargetVelocityRad = 0;
                Active = !Active;
                if (Active) {
                    camera.CustomName = "!" + camera.CustomName;
                    logger.persist($"View {camera.CustomName}");
                } else {
                    camera.CustomName = camera.CustomName.Substring(1);
                }
            }
        }
        Menu ModuleScan(MenuModule aMain = null, object argument = null) {
            CameraModule mod;
            GetModule(out mod);
            if (mod != null) {
                MyDetectedEntityInfo entity;
                ThyDetectedEntityInfo thy;
                if (mod.Scan(camera.WorldMatrix.Translation + camera.WorldMatrix.Forward * range, out entity, out thy)) {
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
                        var mod = controller.Camera;
                        ThyDetectedEntityInfo thy;
                        if (mod == null) {
                            logger.persist("Camera module interface failure."); 
                        } else {
                            logger.persist("Periscope scan success.");
                            mod.AddNew(entity, out thy);
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
                    //logger.log(rot);
                    if (first == null) {
                        logger.log("first null");
                    } else {
                        /* If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
                         * compute the dot product v * (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively. */
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
