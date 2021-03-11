using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

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

            var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);

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
    }
}
