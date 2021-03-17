using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    public class ThyDetectedEntityInfo {

        public static readonly NameGen Namer = new NameGen();
        public readonly long EntityId;
        public string Name { get; private set; }
        public ThyDetectedEntityType Type { get; private set; }
        public readonly Vector3D? HitPosition;
        public readonly MatrixD Orientation;
        public readonly Vector3 Velocity;
        public readonly MyRelationsBetweenPlayerAndBlock Relationship;
        public readonly BoundingBoxD BoundingBox;
        public BoundingSphereD WorldVolume { get; private set; }
        public double TimeStamp { get; private set; }
        public Vector3D Position => BoundingBox.Center;
        public ThyDetectedEntityInfo() { }
        public void Seen() {
            TimeStamp = MAF.time;
        }
        public ThyDetectedEntityInfo(long aEntityId, string aName, ThyDetectedEntityType aType, Vector3D? aHitPosition, MatrixD aOrientation, Vector3 aVelocity, MyRelationsBetweenPlayerAndBlock aRelationship, BoundingBoxD aBoundingBox, double aTimeStamp, BoundingSphereD aSphere) {
            EntityId = aEntityId;
            Name = aName;
            Type = aType;
            HitPosition = aHitPosition;
            Orientation = aOrientation;
            Velocity = aVelocity;
            Relationship = aRelationship;
            BoundingBox = aBoundingBox;
            TimeStamp = aTimeStamp;
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
            BoundingBox = aEntity.BoundingBox;
            TimeStamp = MAF.time;
            WorldVolume = BoundingSphereD.CreateFromBoundingBox(aEntity.BoundingBox);
        }

        public void SetName(string aName) {
            if (aName != null) {
                Name = aName;
            }
        }
        public void SetTimeStamp(double aTime) => TimeStamp = aTime;
        public bool IsClusterable => Type == ThyDetectedEntityType.AsteroidCluster || Type == ThyDetectedEntityType.Asteroid;
        public void Cluster(ThyDetectedEntityInfo aEntity) {
            if (IsClusterable && aEntity.IsClusterable) {
                WorldVolume = WorldVolume.Include(aEntity.WorldVolume);
            }
        }
    }
}
