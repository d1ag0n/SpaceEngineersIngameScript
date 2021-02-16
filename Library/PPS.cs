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
        

        public PPS(Vector3D aPlanet, double aSeaLevel, Vector3D aPosition) {
            Planet = aPlanet;
            Position = aPosition;
            SeaLevel = aSeaLevel;
            var disp = aPosition - aPlanet;
            var dir = disp;
            var mag = dir.Normalize();

            Vector3D.GetAzimuthAndElevation(dir, out Azimuth, out Elevation);

            if (double.IsNaN(Azimuth)) {
                Azimuth = 0;
            } else {
                Azimuth += Math.PI;
            }
            if (double.IsNaN(Elevation)) {                
                Elevation = 0;
            } else {
                Elevation += MathHelper.PiOver2;
            }

            Altitude = mag - SeaLevel;
        }
        public PPS(Vector3D aPlanet, double aSeaLevel, double aAzimuth, double aElevation, double aAltitude) {
            Planet = aPlanet;
            SeaLevel = aSeaLevel;
            Azimuth = aAzimuth;
            Elevation = aElevation;
            Altitude = aAltitude;
            var dir = Vector3D.Zero;            
            Vector3D.CreateFromAzimuthAndElevation(Azimuth == 0 ? 0 : Azimuth - Math.PI, Elevation == 0 ? 0 : Elevation - MathHelper.PiOver2, out dir);
            Position = Planet + dir * (SeaLevel + Altitude);
        }

        public override string ToString() {
            return $"PPS {MathHelper.ToDegrees(Azimuth)}° {MathHelper.ToDegrees(Elevation)}° {Altitude}m";
        }
    }
}
