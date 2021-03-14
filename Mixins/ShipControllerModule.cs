using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;


namespace IngameScript {
    public class ShipControllerModule : Module<IMyShipController> {
        public readonly bool LargeGrid;
        public readonly float GyroSpeed;
        public MyShipVelocities ShipVelocities { get; private set; }
        public IMyShipController _Remote;
        public IMyShipController Remote {
            get {
                if (_Remote == null || !_Remote.IsFunctional || !(_Remote is IMyRemoteControl)) {
                    foreach (var sc in Blocks) {
                        _Remote = sc;
                        if (sc is IMyRemoteControl) {
                            break;
                        }
                    }
                }
                return _Remote;
            }
        }
        IMyShipController _Cockpit;
        public IMyShipController Cockpit {
            get {
                if (_Cockpit == null || !_Cockpit.IsUnderControl) {
                    foreach (var sc in Blocks) {
                        _Cockpit = sc;
                        if (sc.IsUnderControl) {
                            break;
                        }
                    }
                }
                return _Cockpit;
            }
        }
        readonly List<object> mMenuMethods = new List<object>();
        public ShipControllerModule() {
            LargeGrid = ModuleManager.Program.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            GyroSpeed = LargeGrid ? 30 : 60;
            Update = UpdateAction;
            MenuName = "Ship Controller";
            Menu = p => mMenuMethods;
            mMenuMethods.Add(new MenuMethod("Flat Scan", null, (a, b) => {
                if (Update == UpdateAction) {
                    flatScan();
                }
                return null;
            }));
        }
        
        readonly Vector3D[] arCorners = new Vector3D[8];
        void UpdateAction() {
            ShipVelocities = Remote.GetShipVelocities();
            
            // digi, whiplash - https://discord.com/channels/125011928711036928/216219467959500800/819309679863136257
            // var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);

            //var start = Vector3I.One;




//            Update = flatScan(remote.WorldMatrix.Right, remote.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), 3, 3);
            return;
            var grid = ModuleManager.Program.Me.CubeGrid;
            var min = grid.Min;
            var max = grid.Max;

            switch (Remote.Orientation.Forward) {
                case Base6Directions.Direction.Forward:
                case Base6Directions.Direction.Backward:
                    min.Z = 0;
                    max.Z = 0;
                    break;
                case Base6Directions.Direction.Left:
                case Base6Directions.Direction.Right:
                    min.X = 0;
                    max.X = 0;
                    break;
                case Base6Directions.Direction.Up:
                case Base6Directions.Direction.Down:
                    min.Y = 0;
                    max.Y = 0;
                    break;
            }

            logger.log(logger.gps("min", grid.GridIntegerToWorld(min)));
            logger.log(logger.gps("max", grid.GridIntegerToWorld(max)));

            var bb = new BoundingBoxD(
                ((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize,
                ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize
            );
            var m = grid.WorldMatrix;
            var obb = new MyOrientedBoundingBoxD(bb, m);


            obb.GetCorners(arCorners, 0);
            
            for (int i = 0; i < arCorners.Length; i++) {
                logger.log(logger.gps("obb" + i, arCorners[i]));
            }
            

            var sc = Remote;
            if (sc == null) {
                return;
            }
            
        }

        void flatScan() {
            var grid = ModuleManager.Program.Me.CubeGrid;



            switch (Remote.Orientation.Forward) {
                case Base6Directions.Direction.Forward:
                    logger.persist("FORWARD");
                    Update = flatScan(grid.WorldMatrix.Right, grid.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), grid.Max.X - grid.Min.X, grid.Max.Y - grid.Min.Y);
                    break;
                case Base6Directions.Direction.Backward:
                    logger.persist("BACKWARD");
                    Update = flatScan(grid.WorldMatrix.Left, grid.WorldMatrix.Down, grid.GridIntegerToWorld(grid.Max), 3, 3);
                    break;
                case Base6Directions.Direction.Left:
                    logger.persist("LEFT");
                    Update = flatScan(grid.WorldMatrix.Backward, grid.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), 3, 3);
                    break;
                case Base6Directions.Direction.Right:
                    logger.persist("RIGHT");
                    
                    Update = flatScan(grid.WorldMatrix.Forward, grid.WorldMatrix.Down, grid.GridIntegerToWorld(grid.Max), grid.Max.Z - grid.Min.Z, grid.Max.Y - grid.Min.Y);
                    break;
                case Base6Directions.Direction.Up:
                    logger.persist("UP");
                    Update = flatScan(grid.WorldMatrix.Backward, grid.WorldMatrix.Left, grid.GridIntegerToWorld(grid.Max), 3, 3);
                    break;
                case Base6Directions.Direction.Down:
                    logger.persist("DOWN");
                    Update = flatScan(grid.WorldMatrix.Right, grid.WorldMatrix.Backward, grid.GridIntegerToWorld(grid.Min), 3, 3);
                    break;
            }
        }


        Action flatScan(Vector3D right, Vector3D up, Vector3D start, int width, int height) {
            var gsz = ModuleManager.Program.Me.CubeGrid.GridSize;
            int extra = 4;
            start -= right * (gsz * extra);
            start -= up * (gsz * extra);
            width += extra * 2;
            height += extra * 2; ;
            int x = 0;
            int y = 0;
            var sb = new StringBuilder();
            return () => {
                logger.log("flat scanning");
                var t = start + (right * (x * gsz)) + (up * (y * gsz));
                if (x == 0 && y == 0) {
                    sb.AppendLine(logger.gps($"x{x} y{y}", t));
                }
                x++;
                if (x == width) {
                    x = 0;
                    y++;
                    if (y > height) {
                        sb.AppendLine(logger.gps($"x{x} y{y}", t));
                        Remote.CustomData = sb.ToString();
                        logger.persist("Flat Scan Complete");
                        //Update = () => logger.log("flat scan complete");
                        Update = UpdateAction;
                    }
                }
            };
        }
    }
}
