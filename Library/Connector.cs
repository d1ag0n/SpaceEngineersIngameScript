using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage;
using VRageMath;

namespace Library
{
    class Connector
    {
        public long Id;
        public string Name;
        public Vector3D Position;
        public Vector3D Approach;
        public Vector3D FinalApproach;
        public Vector3D Direction;
        public Connector(IMyShipConnector aConnector) {
            Id = aConnector.EntityId;
            Name = aConnector.CustomName;
            Position = aConnector.WorldMatrix.Translation;
            Direction = aConnector.WorldMatrix.Forward;
        }
        public Connector(MyTuple<long, string, Vector3D, Vector3D> aData) {
            Id = aData.Item1;
            Name = aData.Item2;
            Position = aData.Item3;
            Direction = aData.Item4;
        }        
        public MyTuple<long, string, Vector3D, Vector3D> Data() => new MyTuple<long, string, Vector3D, Vector3D>(Id, Name, Position, Direction);
        public static ImmutableArray<MyTuple<long, string, Vector3D, Vector3D>> ToCollection(Connector[] aList) {
            var list = new MyTuple<long, string, Vector3D, Vector3D>[aList.Length];
            for (int i = 0; i < aList.Length; i++) {
                list[i] = aList[i].Data();
            }
            return ImmutableArray.Create(list);
        }
        public static Dictionary<long, Connector> FromCollection(ImmutableArray<MyTuple<long, string, Vector3D, Vector3D>> aCollection) {
            var result = new Dictionary<long, Connector>();
            for (int i = 0; i < aCollection.Length; i++) {
                var c = new Connector(aCollection[i]);
                result[c.Id] = c;
            }
            return result;
        }
        override public string ToString() => Name;
    }
}
