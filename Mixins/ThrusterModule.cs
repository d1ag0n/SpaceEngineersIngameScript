using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    class ThrusterModule : Module<IMyThrust> {
        readonly ThrustList mThrust = new ThrustList();
        readonly List<IMyParachute> mParachutes = new List<IMyParachute>();
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        readonly ThrustList mHydro = new ThrustList();

        public ThrusterModule() {
            Update = OrganizeAction;
            MenuName = "Thrust Controller";
            Menu = (p) => {
                mMenuItems.Clear();

                mMenuItems.Add(new MenuItem($"Add Forward Acceleration {-Acceleration.Z}", () => {
                    Acceleration.Z -= 0.1;
                    if (MAF.nearEqual(Acceleration.Z, 0)) {
                        Acceleration.Z = 0;
                    }
                }));
                mMenuItems.Add(new MenuItem($"Add Backward Acceleration {-Acceleration.Z}", () => {
                    Acceleration.Z += 0.1;
                    if (MAF.nearEqual(Acceleration.Z, 0)) {
                        Acceleration.Z = 0;
                    }
                }));

                mMenuItems.Add(new MenuItem($"Add Left Acceleration {Acceleration.X}", () => {
                    Acceleration.X += 0.1;
                    if (MAF.nearEqual(Acceleration.X, 0)) {
                        Acceleration.X = 0;
                    }
                }));
                mMenuItems.Add(new MenuItem($"Add Right Acceleration {Acceleration.X}", () => {
                    Acceleration.X -= 0.1;
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
            foreach(var t in Blocks) {
                if (GetGroup(t) == enGroup.Hydro) {
                    mHydro.Add(controller.Remote, t);
                } else {
                    mThrust.Add(controller.Remote, t);
                }
            }
            Update = UpdateAction;
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
            return base.Accept(aBlock);
        }
 
        public Vector3D Acceleration;
        void UpdateAction() {
            var a = Acceleration;
            var m = controller.Mass;
            mThrust.Update(ref a, m, false);
        }
    }
}
