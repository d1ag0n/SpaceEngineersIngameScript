using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    public struct Ore {
        public readonly ThyDetectedEntityInfo Thy;
        public readonly string Name;
        public readonly Vector3D Location;
        public Vector3D BestApproach;
        const long grid = 4;
        public Vector3L Index => new Vector3L(
            MAF.round((long)Location.X + (Location.X > 0L ? grid : -grid), grid * 2L),
            MAF.round((long)Location.Y + (Location.Y > 0L ? grid : -grid), grid * 2L),
            MAF.round((long)Location.Z + (Location.Z > 0L ? grid : -grid), grid * 2L)
        );
        public Ore(ThyDetectedEntityInfo aThy, string aName, Vector3D aLocation) {
            Thy = aThy;
            Name = aName;
            Location = aLocation;
            BestApproach = Vector3D.Zero;
        }
        public MyTuple<Vector3D, Vector3D, BoundingSphereD> Pack() => MyTuple.Create(Location, BestApproach, Thy.WorldVolume);
        public static MyTuple<Vector3D, Vector3D, BoundingSphereD> Unpack(object data) => (MyTuple<Vector3D, Vector3D, BoundingSphereD>)data;
    }
}
