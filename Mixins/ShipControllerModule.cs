using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class ShipControllerModule : Module<IMyShipController> {
        public readonly bool LargeGrid;
        public readonly float GyroSpeed;

        public Vector3 MoveIndicator;
        public Vector2 RotationIndicator;
        public float RollIndicator;
        public MyShipVelocities ShipVelocities;
        public MatrixD WorldMatrix;
        public Vector3D NaturalGravity;
        public bool Dampeners;
        public bool HandBrake;
        public Vector3D CenterOfMass;

        IMyShipController ActiveCockpit;

        public ShipControllerModule() {
            LargeGrid = ModuleManager.Program.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            GyroSpeed = LargeGrid ? 30 : 60;
        }

        void FindMainCockpit() {
            ActiveCockpit = null;
            foreach(var sc in Blocks) {
                ActiveCockpit = sc;
                if (ActiveCockpit.IsWorking && ActiveCockpit.IsUnderControl) {
                    return;
                }
            }
        }
        public override void Update() {
            if (ActiveCockpit == null|| !ActiveCockpit.IsWorking || !ActiveCockpit.IsMainCockpit) {
                FindMainCockpit();
            }
            if (ActiveCockpit == null) {
                MoveIndicator = Vector3.Zero;
                RotationIndicator = Vector2.Zero;
                RollIndicator = 0;
                ShipVelocities = default(MyShipVelocities);
                WorldMatrix = MatrixD.Zero;
                NaturalGravity = Vector3D.Zero;
                
            } else {
                MoveIndicator = ActiveCockpit.MoveIndicator;
                RotationIndicator = ActiveCockpit.RotationIndicator;
                RollIndicator = ActiveCockpit.RollIndicator;
                ShipVelocities = ActiveCockpit.GetShipVelocities();
                WorldMatrix = ActiveCockpit.WorldMatrix;
                NaturalGravity = ActiveCockpit.GetNaturalGravity();
                Dampeners = ActiveCockpit.DampenersOverride;
                CenterOfMass = ActiveCockpit.CenterOfMass;
                HandBrake = ActiveCockpit.HandBrake;
            }
            
        }
    }
}