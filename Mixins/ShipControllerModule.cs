using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public class ShipControllerModule : Module<IMyShipController> {
        public readonly bool LargeGrid;
        public readonly float GyroSpeed;
        
        public MissionBase Mission;

        public ThyDetectedEntityInfo Target;
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
        readonly IMyBroadcastListener mMotherState;


        public bool Damp = true;

        public ShipControllerModule() {
            if (!ModuleManager.Mother) {
                mMotherState = ModuleManager.Program.IGC.RegisterBroadcastListener("MotherState");
            }
            MotherLastUpdate = MAF.Epoch;
            LargeGrid = ModuleManager.Program.Me.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large;
            GyroSpeed = LargeGrid ? 30 : 60;

            // if this is changed, UpdateAction needs to work when called before whatever
            // somthing needs to handle call to UpdateAction in ModuleManager.Initialize
            onUpdate = InitializeAction;
            ////////////////////////
            MenuName = "Ship Controller";
            onPage = p => {
                mMenuMethods.Clear();
                CameraModule cam;
                if (GetModule(out cam)) {
                    if (Target == null) {
                        new MenuItem("Flat Scan - No Target Designated");
                    } else {
                        mMenuMethods.Add(new MenuItem($"Flat Scan {Target.Name}", () => {
                            double yaw, pitch;
                            
                            MAF.getRotationAngles(Vector3D.Normalize(Target.Position - Remote.WorldMatrix.Translation), Remote.WorldMatrix, out yaw, out pitch);
                            
                            var act = flatScan(MatrixD.Transform(Remote.WorldMatrix, Quaternion.CreateFromYawPitchRoll((float)yaw, (float)pitch, 0)));
                            //var act = flatScan(Remote.WorldMatrix);
                            StringBuilder sb = new StringBuilder();
                            int i = 0;
                            while (true) {
                                var f = act();
                                if (f == Vector3D.Zero) {
                                    break;
                                }
                                sb.AppendLine(logger.gps($"v{i++}", f));
                            }
                            Remote.CustomData = sb.ToString();
                        }));
                    }
                } else {
                    logger.persist("No camera mod found?");
                }

                mMenuMethods.Add(new MenuItem("Random Mission", () => Mission = new RandomMission(this, new BoundingSphereD(Remote.CenterOfMass + MAF.ranDir() * 1100.0, 0))));

                mMenuMethods.Add(new MenuItem($"Dampeners {Damp}", () => { 
                    Damp = !Damp;
                    if (!Damp) {
                        ThrustModule thrust;
                        if (GetModule(out thrust)) {
                            thrust.Acceleration = Vector3D.Zero;
                        }
                    }
                }));

                mMenuMethods.Add(new MenuItem("Abort Mission", () => {
                    Mission = null;
                    Damp = true;
                }));

                return mMenuMethods;
            };
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
                if (ModuleManager.Mother) {
                    onUpdate = UpdateGlobal;
                } else {
                    onUpdate = UpdateChild;
                }
                onUpdate();
            } else {
                UpdateLocal();
            }
        }
        public long MotherId { get; private set; }
        public BoundingSphereD MotherSphere { get; private set; }
        public Vector3D MotherVeloDir { get; private set; }
        public double MotherSpeed { get; private set; }
        public DateTime MotherLastUpdate { get; private set; }
        public long EntityId => ModuleManager.Program.Me.EntityId;

        void UpdateChild() {
            while (mMotherState.HasPendingMessage) {
                var m = mMotherState.AcceptMessage();
                MotherId = m.Source;
                logger.log("MotherId ", MotherId);
                var ms = MotherShipModule.MotherState(m.Data);
                MotherSphere = ms.Item1;
                MotherVeloDir = ms.Item2;
                MotherSpeed = ms.Item3;
                MotherLastUpdate = MAF.Now;
            }
            UpdateGlobal();
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
            logger.log("Mass ", Mass);
            var lastVelo = ShipVelocities.LinearVelocity;
            ShipVelocities = Remote.GetShipVelocities();
            //logger.log(Remote.CustomName);
            var change = ShipVelocities.LinearVelocity - lastVelo;
            var accel = change.Length() / ModuleManager.Program.Runtime.TimeSinceLastRun.TotalSeconds;

            //logger.log($"Acceleration ", accel);
            var lvd = ShipVelocities.LinearVelocity;
            LinearVelocity = lvd.Normalize();
            if (lvd.IsValid()) {
                LinearVelocityDirection = lvd;
            } else {
                lvd = Vector3D.Zero;
                LinearVelocity = 0;
            }

            LocalLinearVelo = MAF.world2dir(ShipVelocities.LinearVelocity, MyMatrix);
            //logger.log("LocalLinearVelo", LocalLinearVelo);

            
        }
        
        void UpdateGlobal() {
            UpdateLocal();

            if (Target != null) {
                Mission = new Mission(this, Target);
                //logger.persist($"SET NEW MISSION TO {Target.Name}");
                Target = null;
                Damp = false;
            } else if (Mission != null) {

                if (Mission.Complete) {
                    logger.persist("MISSION COMPLETE");
                    Mission = null;
                } else {
                    //logger.log("MISSION UNDERWAY");
                    Mission.Update();
                }
            }
            if (Damp) {
                var localVelo = LocalLinearVelo;
                var localVeloSq = localVelo.LengthSquared();

                if (localVeloSq <= 0.000025) {
                    localVelo = Vector3D.Zero;
                    Damp = false;
                }
                Thrust.Acceleration = localVelo * -6.0;

                /*
                if (GetModule(out gyro)) {
                    if (localVeloSq > 25) {
                        gyro.SetTargetDirection(ShipVelocities.LinearVelocity);
                    } else {
                        gyro.SetTargetDirection(Vector3D.Zero);
                    }
                }*/
            } else {
                //lastPos = Remote.CenterOfMass;
                //estimatedStop = Thrust.StopDistance;
            }
        }
        //Vector3D lastPos;
        //double estimatedStop;
        void zzzzUpdateAction() {
            // maxAcceleration = thrusterThrust / shipMass;
            // boosterFireDuration = Speed / maxAcceleration / 2;
            // minAltitude = Speed * boosterFireDuration;

            
            
            //var start = Vector3I.One;

            // Update = flatScan(remote.WorldMatrix.Right, remote.WorldMatrix.Up, grid.GridIntegerToWorld(grid.Min), 3, 3);
        }
        // digi, whiplash - https://discord.com/channels/125011928711036928/216219467959500800/819309679863136257
        // var bb = new BoundingBoxD(((Vector3D)grid.Min - Vector3D.Half) * grid.GridSize, ((Vector3D)grid.Max + Vector3D.Half) * grid.GridSize);
        // var obb = new MyOrientedBoundingBoxD(bb, grid.WorldMatrix);


        VectorHandler flatScan(MatrixD aMatrix) {
            
            var grid = ModuleManager.Program.Me.CubeGrid;
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
            var gsz = ModuleManager.Program.Me.CubeGrid.GridSize;
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
