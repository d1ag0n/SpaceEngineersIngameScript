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
        public ShipControllerModule() {
            LargeGrid = ModuleManager.Program.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            GyroSpeed = LargeGrid ? 30 : 60;
            Update = UpdateAction;
        }
        
        readonly Vector3D[] arCorners = new Vector3D[8];
        void UpdateAction() {
            var grid = ModuleManager.Program.Me.CubeGrid;
            // digi, whiplash - https://discord.com/channels/125011928711036928/216219467959500800/819309679863136257
            // var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);
            
            //var start = Vector3I.One;
            

            switch (Remote.Orientation.Forward) {
                case Base6Directions.Direction.Forward:
                    Update = flatScan(grid.WorldMatrix.Right, grid.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), 3, 3);
                    break;
                case Base6Directions.Direction.Backward:
                    Update = flatScan(grid.WorldMatrix.Left, grid.WorldMatrix.Down, grid.GridIntegerToWorld(grid.Max), 3, 3);
                    break;
                case Base6Directions.Direction.Left:
                    Update = flatScan(grid.WorldMatrix.Backward, grid.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), 3, 3);
                    break;
                case Base6Directions.Direction.Right:
                    Update = flatScan(grid.WorldMatrix.Forward, grid.WorldMatrix.Down, grid.GridIntegerToWorld(grid.Max), 3, 3);
                    break;
                case Base6Directions.Direction.Up:
                    Update = flatScan(grid.WorldMatrix.Backward, grid.WorldMatrix.Left, grid.GridIntegerToWorld(grid.Max), 3, 3);
                    break;
                case Base6Directions.Direction.Down:
                    Update = flatScan(grid.WorldMatrix.Right, grid.WorldMatrix.Backward, grid.GridIntegerToWorld(grid.Min), 3, 3);
                    break;
            }

//            Update = flatScan(remote.WorldMatrix.Right, remote.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), 3, 3);
            return;

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
            ShipVelocities = sc.GetShipVelocities();
        }


        Action flatScan(Vector3D right, Vector3D up, Vector3D start, int width, int height) {
            int x = 0;
            int y = 0;

            return () => {
                var t = start + (right * x) + (up * y);
                //logger.log($"x={x},y={y}");
                //logger.persist(logger.gps($"x={x},y={y}", t));
                x++;
                if (x == width) {
                    x = 0;
                    y++;
                    if (y == height) {
                        //Update = () => logger.log("flat scan complete");
                        Update = Void;
                    }
                }
            };
        }
    }
}
