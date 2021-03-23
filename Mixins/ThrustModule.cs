using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class ThrustModule : Module<IMyThrust> {
        public readonly ThrustList mThrust = new ThrustList();
        readonly List<IMyParachute> mParachutes = new List<IMyParachute>();
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        readonly ThrustList mHydro = new ThrustList();
        public double FullStop { get; private set; }
        public Vector3 Stop { get; private set; }
        public double StopDistance { get; private set; }
        enum enGroup { Hydro, Ion, Atmos, Not }
        public ThrustModule() {
            onUpdate = InitAction;
             MenuName = "Thrust Controller";
            //*
            onPage = (p) => {
                mMenuItems.Clear();

                var af = Acceleration.Z < 0 ? (-Acceleration.Z).ToString("f1") : "";
                mMenuItems.Add(new MenuItem($"Add Forward Acceleration {af}", () => {
                    var accel = Acceleration;
                    accel.Z -= 0.1;
                    if (MAF.nearEqual(accel.Z, 0)) {
                        accel.Z = 0;
                    }
                    Acceleration = accel;
                }));
                var ab = Acceleration.Z > 0 ?  Acceleration.Z.ToString("f1") : "";
                mMenuItems.Add(new MenuItem($"Add Backward Acceleration {ab}", () => {
                    var accel = Acceleration;
                    accel.Z += 0.1;
                    if (MAF.nearEqual(accel.Z, 0)) {
                        accel.Z = 0;
                    }
                    Acceleration = accel;
                }));
                var al = Acceleration.X < 0 ? (-Acceleration.X).ToString("f1") : "";
                mMenuItems.Add(new MenuItem($"Add Left Acceleration {al}", () => {
                    var accel = Acceleration;
                    accel.X -= 0.1;
                    if (MAF.nearEqual(accel.X, 0)) {
                        accel.X = 0;
                    }
                    Acceleration = accel;
                }));
                var ar = Acceleration.X > 0 ? Acceleration.X.ToString("f1") : "";
                mMenuItems.Add(new MenuItem($"Add Right Acceleration {ar}", () => {
                    var accel = Acceleration;
                    accel.X += 0.1;
                    if (MAF.nearEqual(accel.X, 0)) {
                        accel.X = 0;
                    }
                    Acceleration = accel;
                }));
                var au = Acceleration.Y > 0 ? Acceleration.Y.ToString("f1") : "";
                mMenuItems.Add(new MenuItem($"Add Up Acceleration {au}", () => {
                    var accel = Acceleration;
                    accel.Y += 0.1;
                    if (MAF.nearEqual(accel.Y, 0)) {
                        accel.Y = 0;
                    }
                    Acceleration = accel;
                }));
                var ad = Acceleration.Y < 0 ? (-Acceleration.Y).ToString("f1") : "";
                mMenuItems.Add(new MenuItem($"Add Down Acceleration {ad}", () => {
                    var accel = Acceleration;
                    accel.Y -= 0.1;
                    if (MAF.nearEqual(accel.Y, 0)) {
                        accel.Y = 0;
                    }
                    Acceleration = accel;
                }));
                return mMenuItems;
            };//*/
            //mMenuItems.Add(new MenuMethod())
        }

        public double zPreferredVelocity(double distToTarget, double maxSpeed) => MathHelperD.Clamp((distToTarget * 0.5) / StopDistance, 0, 1.0) * maxSpeed;

        //whiplash says V^2 = 2ad
        public double PreferredVelocity(Vector3D aDir, double dist) {
            var maxAccel = mThrust.MaxAccel(aDir, controller.Mass).Length();
            ModuleManager.logger.log("pvMaxAccel ", maxAccel);
            return 0.99 * Math.Sqrt(2 * maxAccel * dist);
        }

        Vector3D _Acceleration;
        public bool Emergency = false;
        public Vector3D Acceleration {
            get { return _Acceleration; }
            set {
                _Acceleration = value; updateRequired = true;
            }
        }
        enGroup GetGroup(IMyThrust aThrust) {
            switch (aThrust.BlockDefinition.SubtypeName) {
                case "LargeBlockLargeHydrogenThrust":
                case "LargeBlockSmallHydrogenThrust":
                case "SmallBlockLargeHydrogenThrust":
                case "SmallBlockSmallHydrogenThrust":
                    return enGroup.Hydro;
                case "LargeBlockLargeThrust":
                case "LargeBlockSmallThrust":
                case "SmallBlockLargeThrust":
                case "SmallBlockSmallThrust":
                    return enGroup.Ion;
                case "LargeBlockLargeAtmosphericThrust":
                case "LargeBlockSmallAtmosphericThrust":
                case "SmallBlockSmallAtmosphericThrust":
                case "SmallBlockLargeAtmosphericThrust":
                    return enGroup.Atmos;
                default:
                    return enGroup.Not;
            }
        }
        public override bool Accept(IMyTerminalBlock b) {
            if (b is IMyParachute) {
                mParachutes.Add(b as IMyParachute);
                return true;
            }
            return base.Accept(b);
        }
        void InitAction() {
            foreach (var b in Blocks) {
                var g = GetGroup(b);
                if (g == enGroup.Hydro) {
                    mHydro.Add(b);
                } else {
                    mThrust.Add(b);
                }
            }
            onUpdate = UpdateAction;
        }
        bool updateRequired = false;
        void UpdateAction() {
            if (updateRequired) {
                var a = Acceleration;
                var m = controller.Mass;

                mThrust.Update(a, m, Emergency);
                /*logger.log($"FrontForce {mThrust.FrontForce:F0}");
                logger.log($"BackForce  {mThrust.BackForce:F0}");

                logger.log($"LeftForce  {mThrust.LeftForce:F0}");
                logger.log($"RightForce {mThrust.RightForce:F0}");

                logger.log($"UpForce    {mThrust.UpForce:F0}");
                logger.log($"DownForce  {mThrust.DownForce:F0}");*/

                // v^2*m/(2F)
                var llv = controller.LocalLinearVelo;
                var vF = new Vector3D(
                    llv.X > 0 ? mThrust.LeftForce : mThrust.RightForce,
                    llv.Y > 0 ? mThrust.DownForce : mThrust.UpForce,
                    llv.Z > 0 ? mThrust.FrontForce : mThrust.BackForce
                );

                var llvd = Vector3D.Normalize(llv);
                var maxAccel = mThrust.MaxAccel(llvd, m);
                //ModuleManager.logger.log("maxAccel", maxAccel);
                //FullStop = stop(llvd * 100, m, vF).Length();
                FullStop = stop(llvd * 100, maxAccel).Length();
                ModuleManager.logger.log("FullStop", FullStop);
                
                Stop = stop(llv, maxAccel);
                ModuleManager.logger.log("Stop  ", Stop.Length());
                ModuleManager.logger.log("Stop2 ", stop(llv, m, vF).Length());
                StopDistance = Stop.Length();
                //llv.X = stop(llv.X, m, llv.X > 0 ? mThrust.LeftForce : mThrust.RightForce);
                //llv.Y = stop(llv.Y, m, llv.Y > 0 ? mThrust.DownForce : mThrust.UpForce);
                //llv.Z = stop(llv.Z, m, llv.Z > 0 ? mThrust.FrontForce : mThrust.BackForce);

                // a = F / m

                // a = F / m

                /*var vA = new Vector3D(
                    (llv.X > 0 ? mThrust.LeftForce : mThrust.RightForce) / m,
                    (llv.Y > 0 ? mThrust.DownForce : mThrust.UpForce) / m,
                    (llv.Z > 0 ? mThrust.FrontForce : mThrust.BackForce) / m
                );*/
                //var wStop = stop(controller.LocalLinearVelo, vA);

                //logger.log($"Stop       {llv.Length()}");
                //logger.log($"StopDistance {StopDistance:f0}");
                //logger.log($"wStop      {wStop.Length()}");
                //logger.log($"yStop      {yStop}");
                //logger.log($"zStop      {zStop}");
                if (_Acceleration.IsZero()) {
                    updateRequired = false;
                }
            }
        }
        //whiplash says
        //d = V^2/(2*a)
        Vector3D stop(Vector3D V, Vector3D a) => (V * V) / (2.0 * a);
        // 10 = 2 * 5
        // F = m * a

        // 2 = 10 / 5
        // m = F / a

        // 5 = 10 / 2
        // a = F / m

        //double stop(double v, double m, double F) => (v * v) * m / (2 * F);
        Vector3D stop(Vector3D v, double m, Vector3D F) => (v * v) * m / (2 * F);

    }

}
