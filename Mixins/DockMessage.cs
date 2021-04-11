using VRage;
using VRageMath;
using System;
namespace IngameScript
{
struct DockMessage {
        public Vector3I theConnector;
        public Base6Directions.Direction ConnectorFace;
        public DateTime Reserved;
        public Vector3D ConnectorDir => Base6Directions.GetVector(ConnectorFace);

        public bool isReserved => (MAF.Now - Reserved).TotalMinutes < Connector.reserveTime;
        public static DockMessage Unbox(object data) {
            var msg = (MyTuple<Vector3I, int>)data;
            var result = new DockMessage();
            result.theConnector = msg.Item1;
            result.ConnectorFace = (Base6Directions.Direction)msg.Item2;
            return result;
        }
        public MyTuple<Vector3I, int> Box() =>
            MyTuple.Create(theConnector, (int)ConnectorFace);
    }
}