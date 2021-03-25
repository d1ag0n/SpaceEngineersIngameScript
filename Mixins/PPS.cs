using VRageMath;

namespace IngameScript
{
    struct OPS
    {
        public readonly Vector3D Planet;
        public readonly Vector3D Position;
        public readonly Vector3D Direction;
        public readonly double SeaLevel, Azimuth, Elevation, Altitude;

        public OPS(Vector3D aPlanet, double aSeaLevel, Vector3D aPosition) {
            Planet = aPlanet;
            Position = aPosition;
            SeaLevel = aSeaLevel;
            Direction = aPosition - aPlanet;
            Altitude = Direction.Normalize() - SeaLevel;
            Vector3D.GetAzimuthAndElevation(Direction, out Azimuth, out Elevation);
        }
        public OPS(Vector3D aPlanet, double aSeaLevel, double aAzimuth, double aElevation, double aAltitude) {
            Planet = aPlanet;
            SeaLevel = aSeaLevel;
            Azimuth = aAzimuth;
            Elevation = aElevation;
            Altitude = aAltitude;
            Vector3D.CreateFromAzimuthAndElevation(Azimuth, Elevation, out Direction);
            Position = Planet + Direction * (SeaLevel + Altitude);
        }
        public override string ToString() {
            return $"PPS {MathHelper.ToDegrees(Azimuth).ToString("F2")}° {MathHelper.ToDegrees(Elevation).ToString("F2")}° {Altitude.ToString("F2")}m";
        }
    }
}
