using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript
{
    public class BoxMap
    {
        public static readonly double reservationTime = 300.0;
        readonly Dictionary<string, Dictionary<int, BoxInfo>> map = new Dictionary<string, Dictionary<int, BoxInfo>>();

        public BoxInfo getInfo(Vector3D aCBoxCenter) {
            BoxInfo result;
            Vector3D kbox;
            var kmap = getKMap(aCBoxCenter, out kbox);

            var ckey = BOX.CVectorToIndex(aCBoxCenter - kbox);

            if (!kmap.TryGetValue(ckey, out result)) {
                result = new BoxInfo();
                result.Position = aCBoxCenter;
                kmap.Add(ckey, result);
            } else {
                
                if ((MAF.Now - result.Reserved).TotalSeconds > reservationTime) {
                    result.Reserver = 0;
                    kmap[ckey] = result;
                }
            }
            
            return result;
        }
        Dictionary<int, BoxInfo> getKMap(Vector3D aPosition, out Vector3D aKBox) {

            Dictionary<int, BoxInfo> result;

            aKBox = BOX.WorldToK(aPosition);
            var kkey = aKBox.ToString();

            if (!map.TryGetValue(kkey, out result)) {
                result = new Dictionary<int, BoxInfo>();
                map.Add(kkey, result);
            }
            return result;
        }
        void setInfo(BoxInfo info) {
            Vector3D kbox;
            var kmap = getKMap(info.Position, out kbox);
            var ckey = BOX.CVectorToIndex(info.Position - kbox);
            kmap[ckey] = info;
        }
        public BoxInfo dropReservation(long sender, Vector3D aCBoxCenter) {
            var info = getInfo(aCBoxCenter);
            if (sender == info.Reserver) {
                info.Reserver = 0;
                setInfo(info);
            }
            return info;
        }
        public BoxInfo setReservation(long sender, Vector3D aCBoxCenter) {
            var info = getInfo(aCBoxCenter);
            if (info.Reserver == 0) {
                info.Reserved = MAF.Now;
                info.Reserver = sender;
                setInfo(info);
            }
            return info;
        }
        public void reportObstruction(Vector3D aCBoxCenter) {
            var info = getInfo(aCBoxCenter);
            if (!info.Obstructed) {
                info.Obstructed = true;
                setInfo(info);
            }
        }
    }
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
