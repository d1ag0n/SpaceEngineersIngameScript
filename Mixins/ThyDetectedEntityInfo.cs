using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    public class ThyDetectedEntityInfo {

        static HashSet<string> names = new HashSet<string>();
        static readonly NameGen _namer = new NameGen();
        public readonly HashSet<string> mOreTypes = new HashSet<string>();
        readonly HashSet<Vector3L> mOreRegistry = new HashSet<Vector3L>();
        readonly List<MyDetectedEntityInfo> mOres = new List<MyDetectedEntityInfo>();

        // true if type of ore added is new to this asterois/cluster
        public bool AddOre(MyDetectedEntityInfo e) {
            if (e.HitPosition.HasValue) {
                Vector3L pos;
                var hit = e.HitPosition.Value;
                pos.X = (long)hit.X;
                pos.Y = (long)hit.Y;
                pos.Z = (long)hit.Z;
                if (mOreRegistry.Add(pos)) {
                    mOres.Add(e);
                    return mOreTypes.Add(e.Name);
                }
            }
            return false;
        }
        public static string GenerateName() {
            string result;
            do { result = _namer.Next(MAF.random.Next(2, 4)); } while (names.Contains(result));
            names.Add(result);
            return result;
        }
        public readonly long EntityId;
        public string Name { get; private set; }
        public ThyDetectedEntityType Type { get; private set; }
        public Vector3D? HitPosition { get; private set; }
        public MatrixD Orientation { get; private set; }
        public Vector3 Velocity { get; private set; }
        public MyRelationsBetweenPlayerAndBlock Relationship { get; private set; }
        public BoundingSphereD WorldVolume { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public Vector3D Position => WorldVolume.Center;
        public ThyDetectedEntityInfo() { }
        public ThyDetectedEntityInfo(Vector3D aTarget) {
            WorldVolume = new BoundingSphereD(aTarget, 100);
        }
        public void Seen(MyDetectedEntityInfo entity) {
            switch (Type) {
                case ThyDetectedEntityType.Asteroid:
                case ThyDetectedEntityType.AsteroidCluster:
                    WorldVolume = WorldVolume.Include(new BoundingSphereD(entity.HitPosition.Value, 1.0));
                    break;
                default:
                    WorldVolume = BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox);
                    break;
            }
            TimeStamp = DateTime.Now;

            if (entity.HitPosition.HasValue) {
                HitPosition = entity.HitPosition;
            }
            Orientation = entity.Orientation;
            Relationship = entity.Relationship;
            Velocity = entity.Velocity;
            TimeStamp = DateTime.Now;
        }
        public ThyDetectedEntityInfo(long aEntityId, string aName, ThyDetectedEntityType aType, Vector3D? aHitPosition, MatrixD aOrientation, Vector3 aVelocity, MyRelationsBetweenPlayerAndBlock aRelationship, DateTime aDateTime, BoundingSphereD aSphere) {
            EntityId = aEntityId;
            names.Add(aName);
            Name = aName;
            Type = aType;
            HitPosition = aHitPosition;
            Orientation = aOrientation;
            Velocity = aVelocity;
            Relationship = aRelationship;
            TimeStamp = aDateTime;
            WorldVolume = aSphere;
        }

        public  ThyDetectedEntityInfo(MyDetectedEntityInfo aEntity) {
            EntityId = aEntity.EntityId;
            if (aEntity.Type == MyDetectedEntityType.Asteroid) {
                Name = GenerateName();
            } else {
                Name = aEntity.Name;
            }
            
            Type = (ThyDetectedEntityType)aEntity.Type;
            HitPosition = aEntity.HitPosition;
            Orientation = aEntity.Orientation;
            Velocity = aEntity.Velocity;
            Relationship = aEntity.Relationship;
            TimeStamp = DateTime.Now;
            WorldVolume = new BoundingSphereD(aEntity.HitPosition.Value, 1.0);
        }

        public void SetName(string aName) {
            if (aName != null) {
                Name = aName;
            }
        }
        public void SetTimeStamp(DateTime aDateTime) => TimeStamp = aDateTime;
        public bool IsClusterable => Type == ThyDetectedEntityType.AsteroidCluster | Type == ThyDetectedEntityType.Asteroid;
        public void Cluster(BoundingSphereD aSphere) {
            if (Type == ThyDetectedEntityType.Asteroid) {
                Type = ThyDetectedEntityType.AsteroidCluster;
            }
            WorldVolume = WorldVolume.Include(aSphere);
        }
    }
}
