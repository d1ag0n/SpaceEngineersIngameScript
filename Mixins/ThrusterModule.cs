using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    class ThrusterModule : Module<IMyThrust> {
        
        readonly LoggerModule g;
        readonly List<IMyThrust> mIon = new List<IMyThrust>();
        readonly List<IMyThrust> mAtmos = new List<IMyThrust>();
        readonly List<IMyThrust> mHydro = new List<IMyThrust>();

        public enum enGroup {
            Hydro,
            Ion,
            Atmos,
            Not
        }

        public ThrusterModule() {
            GetModule(out g);
        }
        public static enGroup Group(IMyThrust aThrust) {
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
            bool result = base.Accept(b);
            if (result) {
                var t = b as IMyThrust;
                var list = GetList(t);
                if (list != null) {
                    list.Add(t);
                }
            }
            return result;
        }
        List<IMyThrust> GetList(IMyThrust t) {
            List<IMyThrust> list = null;
            switch (Group(t)) {
                case enGroup.Atmos:
                    list = mAtmos;
                    break;
                case enGroup.Hydro:
                    list = mHydro;
                    break;
                case enGroup.Ion:
                    list = mIon;
                    break;
            }
            return list;
        }
        public override bool Remove(IMyTerminalBlock b) {
            var result = base.Remove(b);
            if (result) {

                var t = b as IMyThrust;
                var list = GetList(t);
                if (list != null) {
                    list.Remove(t);
                }

            }
            return result;
        }
        public void Update() {
            g.log("Thruster count=", Blocks.Count);
            foreach (var t in Blocks) {
                //g.log(t.CustomName);
            }

        }
        public IMyThrust Get(int aIndex) {
            IMyThrust result = null;
            if (-1 < aIndex && Blocks.Count > aIndex) {
                result = Blocks[aIndex];
            }
            return result;
        }
    }
}
