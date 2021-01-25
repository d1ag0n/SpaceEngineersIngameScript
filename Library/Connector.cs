using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage;
using VRageMath;

namespace IngameScript
{
    class Connector
    {
        public long ManagerId { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }
        public Vector3D Position { get; private set; }
        public Vector3D Direction { get; private set; }

        public Vector3D Objective;
        public Vector3D Approach;
        public Vector3D ApproachFinal;
        public bool MessageSent = false;
        
        public Connector(IMyProgrammableBlock aBlock, Dock aDock) {
            ManagerId = aBlock.EntityId;
            Id = aDock.X.EntityId;
            Name = aDock.C.CustomName;
            Position = aDock.position;
            Direction = aDock.direction;
        }
        public Connector(MyTuple<long, long, string, Vector3D, Vector3D> aData) {
            ManagerId = aData.Item1;
            Id = aData.Item2;
            Name = aData.Item3;
            Position = aData.Item4;
            Direction = aData.Item5;
        }        
        public MyTuple<long, long, string, Vector3D, Vector3D> Data() => new MyTuple<long, long, string, Vector3D, Vector3D>(ManagerId, Id, Name, Position, Direction);
        public static ImmutableArray<MyTuple<long, long, string, Vector3D, Vector3D>> ToCollection(List<Connector> aList) {
            var list = new MyTuple<long, long, string, Vector3D, Vector3D>[aList.Count];
            for (int i = 0; i < aList.Count; i++) {
                list[i] = aList[i].Data();
            }
            return ImmutableArray.Create(list);
        }
        public static void FromCollection(object aCollection, Dictionary<long, Connector> aDictionary) {
            var collection = (ImmutableArray<MyTuple<long, long, string, Vector3D, Vector3D>>)aCollection;
            for (int i = 0; i < collection.Length; i++) {
                var c = new Connector(collection[i]);
                aDictionary[c.Id] = c;
            }
        }
        override public string ToString() => Name;
    }
}
