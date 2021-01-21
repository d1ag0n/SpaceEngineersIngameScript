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
        public MatrixD World;
        public Vector3D Approach;
        public Vector3D ApproachFinal;
        public Vector3D Objective;
        public Connector(IMyShipConnector aConnector) {
            Id = aConnector.EntityId;
            Name = aConnector.CustomName;
            World = aConnector.WorldMatrix;
        }
        public Connector(MyTuple<long, string, MatrixD> aData) {
            Id = aData.Item1;
            Name = aData.Item2;
            World = aData.Item3;
        }        
        public MyTuple<long, string, MatrixD> Data() => new MyTuple<long, string, MatrixD>(Id, Name, World);
        public static ImmutableArray<MyTuple<long, string, MatrixD>> ToCollection(Connector[] aList) {
            var list = new MyTuple<long, string, MatrixD>[aList.Length];
            for (int i = 0; i < aList.Length; i++) {
                list[i] = aList[i].Data();
            }
            return ImmutableArray.Create(list);
        }
        public static void FromCollection(ImmutableArray<MyTuple<long, string, MatrixD>> aCollection, Dictionary<long, Connector> aDictionary) {
            for (int i = 0; i < aCollection.Length; i++) {
                var c = new Connector(aCollection[i]);
                aDictionary[c.Id] = c;
            }
        }
        override public string ToString() => Name;
    }
}
