using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    public class ThrustModule : Module<IMyThrust> {
        public readonly ThrustList mThrust;
        readonly List<IMyParachute> mParachutes = new List<IMyParachute>();
        readonly ThrustList mHydro;
        public Vector3D FullStop(Vector3D aLocalDirection, Vector3D aMaxAccel) {
            return stop(aLocalDirection * 100, aMaxAccel);
        }
        public Vector3D Stop(Vector3D aMaxAccel) {
            return stop(controller.LocalLinearVelo, aMaxAccel);
        }

        bool _Damp = true;
        public bool Damp {
            get { return _Damp; }
            set {
                if (_Damp != value) {
                    _Acceleration = Vector3D.Zero;
                    _Damp = value;
                    updateRequired = true;
                }
            }
        }

        enum enGroup { Hydro, Ion, Atmos, Not }
        public ThrustModule(ModuleManager aManager):base(aManager) {
            mThrust = new ThrustList(this);
            mHydro = new ThrustList(this);
            onUpdate = InitAction;
            
            /*
            MenuName = "Thrust Controller";
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
        }
        
        //whiplash says V^2 = 2ad
        public Vector3D MaxAccel(Vector3D aLocalVelo) {
            // controller.LocalLinearVelo
            return mThrust.MaxAccel(aLocalVelo, controller.Mass); 
        }
        public double PreferredVelocity(double aMaxAccel, double dist) {
            if (aMaxAccel == 0)
                return 1.0;
            return 0.91 * Math.Sqrt(2 * aMaxAccel * dist);

        }

        Vector3D _Acceleration;
        public bool Emergency = false;
        public Vector3D Acceleration {
            get { return _Acceleration; }
            set {
                if (_Acceleration != value) {
                    _Acceleration = value;
                    updateRequired = true;
                }
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
                b.Enabled = true;
                b.ThrustOverridePercentage = 0f;
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
            if (!Active)
                return;
            if (_Damp) {
                var localVelo = controller.LocalLinearVelo;
                var localVeloSq = localVelo.LengthSquared();
                if (localVeloSq <= 0.000025) {
                    localVelo = Vector3D.Zero;
                }
                Acceleration = localVelo * -6.0;
            }
            if (updateRequired) {
                var a = Acceleration;
                if (a.IsZero()) {
                    updateRequired = false;
                    mThrust.AllStop();
                } else {
                    var m = controller.Mass;
                    mThrust.Update(a, m, Emergency);
                }
            }
        }
        // whiplash says
        // d = V^2/(2*a)
        Vector3D stop(Vector3D V, Vector3D a) => (V * V) / (2.0 * a);


        //double stop(double v, double m, double F) => (v * v) * m / (2 * F);
        Vector3D stop(Vector3D v, double m, Vector3D F) => (v * v) * m / (2 * F);

    }

}
