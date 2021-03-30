using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public class ShipControllerModule : Module<IMyShipController> {
        
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
        

        ATCLientModule ATClient;
        public bool Damp = true;

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
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = base.Accept(aBlock);
            if (result) {
                (aBlock as IMyShipController).DampenersOverride = false;
            }
            return result;
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
                    GetModule(out ATClient);

                    Mission = new CBoxMission(this, ATClient, Volume);
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
                logger.log("Zero velo at");
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
                
                logger.log("Runtime ", mManager.Runtime);
                logger.log("MotherAngularVelo ", MotherAngularVelo);
                logger.log("delta ", d.ToString());
                d += 1.0 / 60.0;
                d += 1.0 / 60.0;
                if (!MotherAngularVelo.IsZero()) {
                    var ng = MotherAngularVelo * d;
                    var len = ng.Normalize();
                    var rot = MatrixD.CreateFromAxisAngle(ng, len);
                    rot.Translation = MotherCoM;
                    var comDisp = m.Translation - MotherCoM;
                    var comDir = comDisp;
                    var comLen = comDir.Normalize();
                    m.Forward = Vector3D.TransformNormal(m.Forward, rot);
                    m.Up = Vector3D.TransformNormal(m.Up, rot);
                    m.Left = Vector3D.TransformNormal(m.Left, rot);
                    comDir = Vector3D.TransformNormal(comDir, rot);
                    m.Translation = MotherCoM + comDir * comLen;
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
            //logger.log("Mass ", Mass);
            //var lastVelo = ShipVelocities.LinearVelocity;
            ShipVelocities = Remote.GetShipVelocities();
            //logger.log(Remote.CustomName);
            //var change = ShipVelocities.LinearVelocity - lastVelo;
            //var accel = change.Length() / ModuleManager.Program.Runtime.TimeSinceLastRun.TotalSeconds;

            //logger.log($"Acceleration ", accel);
            var lvd = ShipVelocities.LinearVelocity;
            LinearVelocity = lvd.Normalize();
            if (!lvd.IsValid()) {
                lvd = Vector3D.Zero;
                LinearVelocity = 0;
            }
            LinearVelocityDirection = lvd;
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
                    //Damp = false;
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
