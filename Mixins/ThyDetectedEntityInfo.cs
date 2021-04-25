using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace IngameScript {

    public class ThyDetectedEntityInfo {

        readonly HashSet<Vector3L> mOreRegistry = new HashSet<Vector3L>();

        public readonly HashSet<string> mOreTypes = new HashSet<string>();
        public readonly List<Ore> mOres = new List<Ore>();
        public readonly long EntityId;
        public string Name;
        public ThyDetectedEntityType Type { get; private set; }
        public Vector3D? HitPosition { get; private set; }
        public MatrixD Orientation { get; private set; }
        public Vector3 Velocity { get; private set; }
        public MyRelationsBetweenPlayerAndBlock Relationship { get; private set; }
        public BoundingSphereD WorldVolume { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public Vector3D Position => WorldVolume.Center;

        // true if type of ore added is new to this asterois/cluster
        public bool AddOre(Ore o) {
            if (mOreRegistry.Add(o.Index)) {
                mOres.Add(o);
                return mOreTypes.Add(o.Name);
            }
            return false;
        }
        public bool AddOre(MyDetectedEntityInfo e) {
            if (e.HitPosition.HasValue) {
                return AddOre(new Ore(this, e.Name, e.HitPosition.Value));
            }
            return false;
        }
        
        
        //public ThyDetectedEntityInfo() { }
        /*public ThyDetectedEntityInfo(Vector3D aTarget) {
            WorldVolume = new BoundingSphereD(aTarget, 100);
        }*/
        public void Seen(MyDetectedEntityInfo entity) {
            switch (Type) {
                case ThyDetectedEntityType.Asteroid:
                case ThyDetectedEntityType.AsteroidCluster:
                    WorldVolume = WorldVolume.Include(new BoundingSphereD(entity.HitPosition.Value, 10.0));
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

            Name = aName;
            Type = aType;
            HitPosition = aHitPosition;
            Orientation = aOrientation;
            Velocity = aVelocity;
            Relationship = aRelationship;
            TimeStamp = aDateTime;
            WorldVolume = aSphere;
        }

        public IEnumerator<bool> asteroidIdentifier(CameraModule aCam) {
            var radiusInc = 25d;
            var radius = radiusInc;
            var angleInc = MathHelperD.Pi / 9d;
            var angle = 0d;
            int hits = 0;
            MatrixD m;
            Vector3D dir2rock;
            Vector3D perp;
            Vector3D scan;
            var entity = new MyDetectedEntityInfo();
            ThyDetectedEntityInfo thy;
            while (true) {
                angle += angleInc;
                
                if (angle > MathHelperD.TwoPi) {
                    angle = 0;
                    radius += radiusInc;
                    if (hits == 0) {
                        yield return false;
                    }
                    if (radius > 1000) {
                        yield return false;
                    }
                    hits = 0;
                }
                dir2rock = WorldVolume.Center - aCam.Volume.Center;
                Vector3D.Normalize(ref dir2rock, out dir2rock);
                dir2rock.CalculatePerpendicularVector(out perp);
                MatrixD.CreateFromAxisAngle(ref dir2rock, angle, out m);
                Vector3D.Rotate(ref perp, ref m, out perp);
                scan = WorldVolume.Center + perp * radius;
                while (!aCam.Scan(ref scan, ref entity, out thy, 250d)) {
                    yield return true;
                }
                if (entity.HitPosition.HasValue) {
                    hits++;
                }
                yield return true;
            }
        }

        public  ThyDetectedEntityInfo(MyDetectedEntityInfo aEntity) {
            EntityId = aEntity.EntityId;
            Name = aEntity.Name;
            Type = (ThyDetectedEntityType)aEntity.Type;
            HitPosition = aEntity.HitPosition;
            Orientation = aEntity.Orientation;
            Velocity = aEntity.Velocity;
            Relationship = aEntity.Relationship;
            TimeStamp = DateTime.Now;
            WorldVolume = new BoundingSphereD(aEntity.HitPosition.Value, 10.0);
            WorldVolume = WorldVolume.Include(new BoundingSphereD(aEntity.BoundingBox.Center, 10.0));
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
        static readonly Dictionary<string, int> mOreWork = new Dictionary<string, int>();
        public void SortOre() {
            mOres.Sort((a, b) => (Position - b.Location).LengthSquared() > (Position - a.Location).LengthSquared() ? -1 : 1);
            int count;
            for (int i = 0; i < mOres.Count; i++) {
                var o = mOres[i];
                if (mOreWork.TryGetValue(o.Name, out count)) {
                    if (count == 10) {
                        mOres.RemoveAt(i--);
                    } else {
                        mOreWork[o.Name] = i + 1;
                    }
                } else {
                    mOreWork[o.Name] = 1;
                }
            }
            mOreTypes.Clear();
            foreach (var o in mOreWork.Keys) {
                mOreTypes.Add(o);
            }
            mOreWork.Clear();
        }
    }
}
