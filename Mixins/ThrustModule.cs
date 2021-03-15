using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    class ThrustModule : Module<IMyThrust> {
        readonly ThrustList mThrust = new ThrustList();
        readonly List<IMyParachute> mParachutes = new List<IMyParachute>();
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        readonly ThrustList mHydro = new ThrustList();
        

        public ThrustModule() {
            onUpdate = OrganizeAction;
            MenuName = "Thrust Controller";
            onPage = (p) => {
                mMenuItems.Clear();

                mMenuItems.Add(new MenuItem($"Add Forward Acceleration {Acceleration.Z}", () => {
                    Acceleration.Z -= 0.1;
                    if (MAF.nearEqual(Acceleration.Z, 0)) {
                        Acceleration.Z = 0;
                    }
                }));
                mMenuItems.Add(new MenuItem($"Add Backward Acceleration {Acceleration.Z}", () => {
                    Acceleration.Z += 0.1;
                    if (MAF.nearEqual(Acceleration.Z, 0)) {
                        Acceleration.Z = 0;
                    }
                }));

                mMenuItems.Add(new MenuItem($"Add Left Acceleration {Acceleration.X}", () => {
                    Acceleration.X -= 0.1;
                    if (MAF.nearEqual(Acceleration.X, 0)) {
                        Acceleration.X = 0;
                    }
                }));
                mMenuItems.Add(new MenuItem($"Add Right Acceleration {Acceleration.X}", () => {
                    Acceleration.X += 0.1;
                    if (MAF.nearEqual(Acceleration.X, 0)) {
                        Acceleration.X = 0;
                    }
                }));

                mMenuItems.Add(new MenuItem($"Add Up Acceleration {Acceleration.Y}", () => {
                    Acceleration.Y += 0.1;
                    if (MAF.nearEqual(Acceleration.Y, 0)) {
                        Acceleration.Y = 0;
                    }
                }));
                mMenuItems.Add(new MenuItem($"Add Down Acceleration {Acceleration.Y}", () => {
                    Acceleration.Y -= 0.1;
                    if (MAF.nearEqual(Acceleration.Y, 0)) {
                        Acceleration.Y = 0;
                    }
                }));
                return mMenuItems;
            };
            //mMenuItems.Add(new MenuMethod())
        }
        void OrganizeAction() {
            onUpdate = UpdateAction;
        }
        enum enGroup { Hydro, Ion, Atmos, Not }
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
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is IMyParachute) {
                mParachutes.Add(aBlock as IMyParachute);
                return true;
            }
            if (aBlock is IMyThrust) {
                var t = aBlock as IMyThrust;
                if (GetGroup(t) == enGroup.Hydro) {
                    mHydro.Add(controller.Remote, t);
                } else {
                    mThrust.Add(controller.Remote, t);
                }
                return true;
            }
            return false;
        }

        public Vector3D Acceleration;
        void UpdateAction() {
            var a = Acceleration;
            var m = controller.Mass;
            mThrust.Update(ref a, m, false);
            logger.log($"FrontForce {mThrust.FrontForce:F0}");
            logger.log($"BackForce  {mThrust.BackForce:F0}");

            logger.log($"LeftForce  {mThrust.LeftForce:F0}");
            logger.log($"RightForce {mThrust.RightForce:F0}");

            logger.log($"UpForce    {mThrust.UpForce:F0}");
            logger.log($"DownForce  {mThrust.DownForce:F0}");

            // v^2*m/(2F)
            var llv = controller.LocalLinearVelo;
            var vF = new Vector3D(
                llv.X > 0 ? mThrust.LeftForce : mThrust.RightForce, 
                llv.Y > 0 ? mThrust.DownForce : mThrust.UpForce, 
                llv.Z > 0 ? mThrust.FrontForce : mThrust.BackForce
            );
            var vstop = stop(llv, m, vF);

            llv.X = stop(llv.X, m, llv.X > 0 ? mThrust.LeftForce : mThrust.RightForce);
            llv.Y = stop(llv.Y, m, llv.Y > 0 ? mThrust.DownForce : mThrust.UpForce);
            llv.Z = stop(llv.Z, m, llv.Z > 0 ? mThrust.FrontForce : mThrust.BackForce);
            
            // a = F / m
            
            // a = F / m

            var vA = new Vector3D(
                (llv.X > 0 ? mThrust.LeftForce : mThrust.RightForce) / m,
                (llv.Y > 0 ? mThrust.DownForce : mThrust.UpForce) / m,
                (llv.Z > 0 ? mThrust.FrontForce : mThrust.BackForce) / m
            );
            var wStop = stop(controller.LocalLinearVelo, vA);

            logger.log($"Stop       {llv.Length()}");
            logger.log($"vStop      {vstop.Length()}");
            logger.log($"wStop      {wStop.Length()}");
            //logger.log($"yStop      {yStop}");
            //logger.log($"zStop      {zStop}");

        }
        //whiplash says
        //d = V^2/(2*a)
        Vector3D stop(Vector3D V, Vector3D a) {
            return (V * V) / (2.0 * a);
        }
        // 10 = 2 * 5
        // F = m * a

        // 2 = 10 / 5
        // m = F / a

        // 5 = 10 / 2
        // a = F / m










        double stop(double v, double m, double F) => (v * v) * m / (2 * F);
        Vector3D stop(Vector3D v, double m, Vector3D F) => (v * v) * m / (2 * F);

    }

}
