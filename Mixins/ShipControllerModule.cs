using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using System.Xml.Serialization;

namespace IngameScript {
    
    public class ShipControllerModule : Module<IMyShipController> {

        bool _mManual;
        public bool mManual {
            get {
                return _mManual;
            }
            set {
                if (OnMission)
                    throw new Exception();
                _mManual = value;
                mGyro.Active = !mManual;
                mThrust.Active = !mManual;
                mThrust.Damp = !mManual;
                AllDampers(mManual);
            }
        }
        public void AbortAllMissions() => NewMission(null);
        
        public readonly float GyroSpeed;
        ThrustModule mThrust;
        GyroModule mGyro;
        readonly Stack<MissionBase> mMissionStack = new Stack<MissionBase>();
        public MissionBase mMission {
            get; private set;
        }

        readonly List<IMyInventory> mInventory = new List<IMyInventory>();
        public MyShipVelocities ShipVelocities { get; private set; }
        public Vector3D LinearVelocityDirection { get; private set; }
        public double LinearVelocity { get; private set; }
        public IMyShipController Remote { get; private set; }
        public IMyShipController Cockpit { get; private set; }
        // todo
        //readonly List<MenuItem> mMenuMethods = new List<MenuItem>();
        public double Mass { get; private set; }
        public Vector3D LocalLinearVelo { get; private set; }

        //public ThrustModule Thrust { get; private set; }
        //public GyroModule Gyro { get; private set; }
        //public CameraModule Camera { get; private set; }
        
        
        //ATCLientModule ATClient;

        public void ExtendMission(MissionBase m) {
            if (mMission != null) {
                mMissionStack.Push(mMission);
                mLog.persist($"Stacked {mMission}");
            } else {
                mManual = false;
            }
            mMission = m;
        }
        public void CancelMission() {
            if (mMission != null) {
                mMission.Cancel();
                mMission = null;
            }
        }
        public void ReplaceMission(MissionBase m) {
            if (mMission != null) {
                mMission.Cancel();
                mMission = null;
            }
            if (m != null) {
                mManual = false;
                mMission = m;
            }
        }
        public void NewMission(MissionBase m) {
            if (mMission != null) {
                mMission.Cancel();
                mMission = null;
            }
            while (mMissionStack.Count > 0) {
                mMissionStack.Pop().Cancel();
            }
            if (m != null) {
                mManual = false;
                mMission = m;
            }
        }



        public ShipControllerModule(ModuleManager aManager) :base (aManager) {
            
            Active = true;

            
            
            GyroSpeed = aManager.LargeGrid ? 30f : 60f;

            // if this is changed, UpdateAction needs to work when called before whatever
            // somthing needs to handle call to UpdateAction in ModuleManager.Initialize
            onUpdate = InitializeAction;
            ////////////////////////
            
            
        }
        public void AllDampers(bool value) {
            foreach (var sc in Blocks) {
                sc.DampenersOverride = value;
            }
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = base.Accept(aBlock);
            if (result) {
                
            } else {
                if (!(aBlock is IMyPowerProducer)) {

                    var inv = aBlock.GetInventory();

                    if (inv != null && (float)inv.MaxVolume > 0f) {
                        mInventory.Add(inv);
                    }
                }
            }
            
            return result;
        }
        // todo change this later to something useful
        public void cargoDetail(Dictionary<string, float> detail) {
            foreach (var inv in mInventory) {
                int index = 0;
                MyInventoryItem? item;
                float val;
                while (true) {
                    item = inv.GetItemAt(index++);
                    if (item.HasValue) {
                        var amount = (float)item.Value.Amount;
                        var name = item.Value.Type.ToString();
                        mLog.persist($"{name} {amount}");
                        if (detail.TryGetValue(name, out val)) {
                            detail[name] = amount - val;
                        } else {
                            detail.Add(name, amount);
                        }
                    } else {
                        break;
                    }
                }
            }
        }
        public float cargoLevel() {
            float c = 0f, m = 0f;
            foreach (var inv in mInventory) {
                
                c += (float)inv.CurrentVolume;
                m += (float)inv.MaxVolume;
            }
            var v = c / m;
            mLog.log($"Cargo Level {v * 100d:f0}%");
            return v;
        }
        void InitializeAction() {
            AllDampers(false);
            mManager.GetModule(out mThrust);
            mManager.GetModule(out mGyro);
            onUpdate = UpdateGlobal;
        }
        
        public bool OnMission => mMission != null;
        public long EntityId => mManager.mProgram.Me.EntityId;



        void UpdateLocal() {
   
            if (Remote == null || !Remote.IsFunctional || !(Remote is IMyRemoteControl)) {
                foreach (var sc in Blocks) {
                    Remote = sc;
                    if (sc is IMyRemoteControl) {
                        break;
                    }
                }
            }
            if (Cockpit == null || !Cockpit.IsFunctional || !Cockpit.IsUnderControl) {
                foreach (var sc in Blocks) {
                    Cockpit = sc;
                    if (sc.IsUnderControl && sc.IsFunctional) {
                        break;
                    }
                }
            }
            if (Remote == null) {
                mLog.log("No ship controller.");
                mManager.mProgram.Echo("No ship controller");
                return;
            }
            var sm = Remote.CalculateShipMass();
            Mass = sm.PhysicalMass;
            ShipVelocities = Remote.GetShipVelocities();
            var lvd = ShipVelocities.LinearVelocity;
            LinearVelocity = lvd.Normalize();
            if (!lvd.IsValid()) {
                lvd = Vector3D.Zero;
                LinearVelocity = 0;
            }
            LinearVelocityDirection = lvd;
            LocalLinearVelo = MAF.world2dir(ShipVelocities.LinearVelocity, MyMatrix);
        }
        
        void UpdateGlobal() {
            UpdateLocal();

            if (mMission == null || mMission.Complete) {
                if (mMissionStack.Count != 0) {
                    mMission = mMissionStack.Pop();
                } else {
                    mMission = null;
                }
            }
            if (mMission != null) {
                mLog.log($"Mission={mMission}");
                mMission.Update();
            }
        }
        //Vector3D lastPos;
        //double estimatedStop;

        // digi, whiplash - https://discord.com/channels/125011928711036928/216219467959500800/819309679863136257
        // var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);
        // var obb = new MyOrientedBoundingBoxD(bb, grid.WorldMatrix);


        /*VectorHandler flatScan(MatrixD aMatrix) {
            
            var grid = mManager.mProgram.Me.CubeGrid;
            Vector3D start = Vector3D.Transform(grid.Min * grid.GridSize, aMatrix);
            int width = 0, height = 0;
            switch (Remote.Orientation.Forward) {
                case Base6Directions.Direction.Forward:
                    logger.persist("FRONT");
                    width = grid.Max.X - grid.Min.X;
                    height = grid.Max.Y - grid.Min.Y;
                    break;
                case Base6Directions.Direction.Backward:
                    logger.persist("BACK");
                    width = grid.Max.X - grid.Min.X;
                    height = grid.Max.Y - grid.Min.Y;
                    break;
                case Base6Directions.Direction.Left:
                    logger.persist("LEFT");
                    width = grid.Max.Z - grid.Min.Z;
                    height = grid.Max.Y - grid.Min.Y;
                    break;
                case Base6Directions.Direction.Right:
                    logger.persist("RIGHT");
                    width = grid.Max.Z - grid.Min.Z;
                    height = grid.Max.Y - grid.Min.Y;
                    break;
                case Base6Directions.Direction.Up:
                    logger.persist("UP");
                    width = grid.Max.Z - grid.Min.Z;
                    height = grid.Max.X - grid.Min.X;
                    break;
                case Base6Directions.Direction.Down:
                    logger.persist("DOWN");
                    width = grid.Max.X - grid.Min.X;
                    height = grid.Max.Z - grid.Min.Z;
                    break;
            }
            width = 3;
            height = 3;
            return flatScan(aMatrix.Right, aMatrix.Up, start, width, height);
        }*/


        /*VectorHandler flatScan(Vector3D right, Vector3D up, Vector3D start, int width, int height) {
            var gsz = mManager.mProgram.Me.CubeGrid.GridSize;
            int extra = 0;
            start -= right * (gsz * extra);
            start -= up * (gsz * extra);
            width += extra * 2;
            height += extra * 2; ;
            int x = width - 1;
            int y = -1;
            
            return () => {
                x++;
                if (x == width) {
                    x = 0;
                    y++;
                    if (y > height) {
                        return Vector3D.Zero;
                    }
                }
                return start + (right * (x * gsz)) + (up * (y * gsz));
            };
        }*/
    }
}
