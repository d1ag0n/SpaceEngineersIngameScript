using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    public class ThyDetectedEntityInfo {

        public static readonly NameGen Namer = new NameGen();
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
                case ThyDetectedEntityType.Planet:
                    WorldVolume = WorldVolume.Include(BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox));
                    break;
                default:
                    WorldVolume = BoundingSphereD.CreateFromBoundingBox(entity.BoundingBox);
                    break;
            }
            
            if (entity.HitPosition.HasValue) {
                HitPosition = entity.HitPosition;
            }
            Orientation = entity.Orientation;
            Relationship = entity.Relationship;
            Velocity = entity.Velocity;
            TimeStamp = MAF.time;
        }
        public ThyDetectedEntityInfo(long aEntityId, string aName, ThyDetectedEntityType aType, Vector3D? aHitPosition, MatrixD aOrientation, Vector3 aVelocity, MyRelationsBetweenPlayerAndBlock aRelationship, DateTime aDateTime, BoundingSphereD aSphere) {
            EntityId = aEntityId;
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
                Name = Namer.Next(MAF.random.Next(1,4));
            } else {
                Name = aEntity.Name;
            }
            
            Type = (ThyDetectedEntityType)aEntity.Type;
            HitPosition = aEntity.HitPosition;
            Orientation = aEntity.Orientation;
            Velocity = aEntity.Velocity;
            Relationship = aEntity.Relationship;
            TimeStamp = MAF.time;
            WorldVolume = BoundingSphereD.CreateFromBoundingBox(aEntity.BoundingBox);
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
