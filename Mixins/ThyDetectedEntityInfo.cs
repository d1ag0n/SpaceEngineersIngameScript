using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    public class ThyDetectedEntityInfo {
        public readonly long EntityId;
        public string Name { get; private set; }
        public readonly MyDetectedEntityType Type;
        public readonly Vector3D? HitPosition;
        public readonly MatrixD Orientation;
        public readonly Vector3 Velocity;
        public readonly MyRelationsBetweenPlayerAndBlock Relationship;
        public readonly BoundingBoxD BoundingBox;
        public double TimeStamp { get; private set; }
        public Vector3D Position => BoundingBox.Center;
        public ThyDetectedEntityInfo(long aEntityId, string aName, MyDetectedEntityType aType, Vector3D? aHitPosition, MatrixD aOrientation, Vector3 aVelocity, MyRelationsBetweenPlayerAndBlock aRelationship, BoundingBoxD aBoundingBox, double aTimeStamp) {
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
            Name = aEntity.Name;
            Type = aEntity.Type;
            HitPosition = aEntity.HitPosition;
            Orientation = aEntity.Orientation;
            Velocity = aEntity.Velocity;
            Relationship = aEntity.Relationship;
            BoundingBox = aEntity.BoundingBox;
            TimeStamp = MAF.time;
        }

        public void SetName(string aName) {
            if (aName != null) {
                Name = aName;
            }
        }
        public void SetTimeStamp(double aTime) => TimeStamp = aTime;
    }
}
