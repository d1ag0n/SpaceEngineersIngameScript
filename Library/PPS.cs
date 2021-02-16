using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    class PPS
    {
        public readonly Vector3D Planet;
        public readonly Vector3D Position;
        public readonly double SeaLevel, Azimuth, Elevation, Altitude;

        // elfie wolfe says
        // shipVector = shipControllers[0].GetShipVelocities().LinearVelocity;
        // shipToGravityVector = VectorProjection(shipVector, gravityVector);
        // shipToHorizontalVector = (shipVector - shipToGravityVector);

        public PPS(Vector3D aPlanet, double aSeaLevel, Vector3D aPosition) {
            Planet = aPlanet;
            Position = aPosition;
            SeaLevel = aSeaLevel;
            var dir = aPosition - aPlanet;
            Altitude = dir.Normalize() - SeaLevel;
            Vector3D.GetAzimuthAndElevation(dir, out Azimuth, out Elevation);
        }
        public PPS(Vector3D aPlanet, double aSeaLevel, double aAzimuth, double aElevation, double aAltitude) {
            Planet = aPlanet;
            SeaLevel = aSeaLevel;
            Azimuth = aAzimuth;
            Elevation = aElevation;
            Altitude = aAltitude;
            Vector3D dir;
            Vector3D.CreateFromAzimuthAndElevation(Azimuth, Elevation, out dir);
            Position = Planet + dir * (SeaLevel + Altitude);
        }
        public override string ToString() {
            return $"PPS {MathHelper.ToDegrees(Azimuth).ToString("N2")}° {MathHelper.ToDegrees(Elevation).ToString("N2")}° {Altitude.ToString("N2")}m";
        }
    }
}
