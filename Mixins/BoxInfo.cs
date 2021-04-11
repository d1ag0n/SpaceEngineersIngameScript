using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript {

    public struct BoxInfo {
        public long Reserver;
        public DateTime Reserved;
        public bool Obstructed;
        public Vector3D Position;
        public MyTuple<long, long, bool, Vector3D> Box() => MyTuple.Create(Reserver, Reserved.ToBinary(), Obstructed, Position);
        public static BoxInfo Unbox(object data) {
            var t = (MyTuple<long, long, bool, Vector3D>)data;
            BoxInfo result;
            result.Reserver = t.Item1;
            result.Reserved = DateTime.FromBinary(t.Item2);
            result.Obstructed = t.Item3;
            result.Position = t.Item4;
            return result;
        }
        public BoundingBoxD CBox => BOX.GetCBox(Position);
        public bool ReservationValid => (MAF.Now - Reserved).TotalSeconds < BoxMap.reservationTime;
        public bool IsReservedBy(long id) => Reserver == id && ReservationValid;
    }
}
