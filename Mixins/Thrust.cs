using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    class Thrust {
        
        public readonly IMyThrust Engine;
        public readonly enGroup Group;
        public Base6Directions.Direction Direction;
        public enum enGroup {
            Hydro,
            Ion,
            Atmos,
            Not
        }
        static enGroup GetGroup(IMyThrust aThrust) {
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
        public Thrust(IMyThrust aThrust) {
            Engine = aThrust;
            Group = GetGroup(aThrust);
        }
    }
}
