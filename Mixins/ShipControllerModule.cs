using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using System.Xml.Serialization;

namespace IngameScript {
    
    public class ShipControllerModule : Module<IMyShipController> {
        
        public readonly float GyroSpeed;

        readonly Queue<MissionBase> mMissionQ = new Queue<MissionBase>();
        MissionBase mMission;

        readonly List<IMyCargoContainer> mCargo = new List<IMyCargoContainer>();
        public MyShipVelocities ShipVelocities { get; private set; }
        public Vector3D LinearVelocityDirection { get; private set; }
        public double LinearVelocity { get; private set; }
        public IMyShipController Remote { get; private set; }
        public IMyShipController Cockpit { get; private set; }
        readonly List<MenuItem> mMenuMethods = new List<MenuItem>();
        public double Mass { get; private set; }
        public Vector3D LocalLinearVelo { get; private set; }

        public ThrustModule Thrust { get; private set; }
        public GyroModule Gyro { get; private set; }
        public CameraModule Camera { get; private set; }
        
        
        ATCLientModule ATClient;

        public void ExtendMission(MissionBase m) {
            if (mMission != null) {
                mMissionQ.Enqueue(mMission);
            }
            mMission = m; 
        }
        public void ReplaceMission(MissionBase m) {
            mMission = m;
        }
        public void NewMission(MissionBase m) {
            mMissionQ.Clear();
            mMission = m;
        }



        public ShipControllerModule(ModuleManager aManager) :base (aManager) {
            if (!aManager.Mother) {
                aManager.mIGC.SubscribeBroadcast("MotherState", onMotherState);
            }

            
            
            GyroSpeed = aManager.LargeGrid ? 30f : 60f;

            // if this is changed, UpdateAction needs to work when called before whatever
            // somthing needs to handle call to UpdateAction in ModuleManager.Initialize
            onUpdate = InitializeAction;
            ////////////////////////
            MenuName = "Ship Controller";
            onPage = p => {
                mMenuMethods.Clear();
                

                //mMenuMethods.Add(new MenuItem("Random Mission", () => Mission = new RandomMission(this, new BoundingSphereD(Remote.CenterOfMass + MAF.ranDir() * 1100.0, 0))));

                mMenuMethods.Add(new MenuItem($"Dampeners {Thrust.Damp}", () => { 
                    Thrust.Damp = !Thrust.Damp;
                }));

                mMenuMethods.Add(new MenuItem("Abort All Missions", () => {
                    mMissionQ.Clear();
                    mMission = null;
                    Thrust.Damp = true;
                }));

                return mMenuMethods;
            };
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is IMyCargoContainer) {
                mCargo.Add(aBlock as IMyCargoContainer);
                return true;
            }
            var result = base.Accept(aBlock);
            if (result) {
                (aBlock as IMyShipController).DampenersOverride = false;
            }
            return result;
        }
        public float cargoLevel() {
            float c = 0f, m = 0f;
            foreach (var cargo in mCargo) {
                var i = cargo.GetInventory();
                c += (float)i.CurrentVolume;
                m += (float)i.MaxVolume;

            }
            var v = c / m;
            logger.log($"Cargo Level {v * 100d:f0}%");
            return v;
        }
        void InitializeAction() {
            var result = true;
            if (Gyro == null) {
                GyroModule gy;
                if (!GetModule(out gy)) {
                    result = false;
                }
                Gyro = gy;
            }
            if (Thrust == null) {
                ThrustModule th;
                if (!GetModule(out th)) {
                    result = false;
                }
                Thrust = th;
            }
            if (Camera == null) {
                CameraModule cam;
                if (!GetModule(out cam)) {
                    result = false;
                }
                Camera = cam;
            }
            if (result) {
                if (mManager.Drill) {
                    if (ATClient == null) {
                        GetModule(out ATClient);
                    }
                    if (ATClient.connected) {
                        Thrust.Damp = false;
                    } else {
                        
                        //Mission = new DockMission(this, ATClient, Volume);
                        
                    }
                }
                onUpdate = UpdateGlobal;
                onUpdate();
            } else {
                UpdateLocal();
            }
        }
        public long MotherId { get; private set; }

        public BoundingSphereD MotherSphere => BoundingSphereD.CreateFromBoundingBox(MotherBox);
        public BoundingBoxD MotherBox { get; private set; }
        public Vector3D MotherCoM { get; private set; } 
        public Vector3D MotherVeloDir { get; private set; }
        public double MotherSpeed { get; private set; }
        public double MotherLastUpdate { get; private set; }


        public Vector3D MotherVeloAt(Vector3D aPos) {
            if (MotherAngularVelo.IsZero()) {
                return Vector3D.Zero;    
            }
            return MotherAngularVelo.Cross(MAF.local2pos(aPos, _MotherMatrix) - MotherCoM);
        }

        MatrixD _MotherMatrix;
        public MatrixD MotherMatrix {
            get {
                // Whiplash141 - https://discord.com/channels/125011928711036928/216219467959500800/825805636691951626
                // cross(angVel, displacement)
                // Where angVel is in rad / s and displacement is measured from the CoM pointing towards the point of interest
                var m = _MotherMatrix;
                //logger.log(logger.gps("Position", _MotherMatrix.Translation));
                var d = mManager.Runtime - MotherLastUpdate;
                //d += 0.0166;
                
                d += 1.0 / 60.0;
                if (!MotherAngularVelo.IsZero()) {
                    var ng = MotherAngularVelo * d;
                    var len = ng.Normalize();
                    var rot = MatrixD.CreateFromAxisAngle(ng, len);
                    var comDisp = m.Translation - MotherCoM;
                    m *= rot;
                    m.Translation = MotherCoM + Vector3D.Transform(comDisp, rot);
                }
                m.Translation += MotherVeloDir * (MotherSpeed * d);
                return m;
            }
            private set {
                _MotherMatrix = value;
            }
        }
        public Vector3D MotherAngularVelo { get; private set; }
        public long EntityId => mManager.mProgram.Me.EntityId;

        void onMotherState(IGC.Envelope e) {
            MotherId = e.Message.Source;
            logger.log("MotherId ", MotherId);
            var ms = MotherShipModule.MotherState(e.Message.Data);
            MotherBox = ms.Item1;
            MotherVeloDir = ms.Item2;
            MotherSpeed = ms.Item3;
            logger.log("MotherSpeed ", MotherSpeed);
            MotherAngularVelo = ms.Item4;
            MotherMatrix = ms.Item5;
            MotherCoM = ms.Item6;
            MotherLastUpdate = e.Time;
        }

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
            if (mManager.Drill && mMission == null) {
                mMission = new DrillMission(this, default(Vector3D));
            }
            if (mMission == null || mMission.Complete) {
                mMissionQ.TryDequeue(out mMission);
            }
            mMission?.Update();
        }
        //Vector3D lastPos;
        //double estimatedStop;

        // digi, whiplash - https://discord.com/channels/125011928711036928/216219467959500800/819309679863136257
        // var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);
        // var obb = new MyOrientedBoundingBoxD(bb, grid.WorldMatrix);


        VectorHandler flatScan(MatrixD aMatrix) {
            
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
        }


        VectorHandler flatScan(Vector3D right, Vector3D up, Vector3D start, int width, int height) {
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
        }
    }
}
