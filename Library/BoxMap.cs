using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript
{
    // this might not be a good name for this class
    class BoxMap
    {
        const double reservationTime = 5 * 60;
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
            }

            if (result.Reserved + reservationTime < MAF.time) {
                result.Reserved =
                result.Reserver = 0;
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
        public void dropReservation(long sender, Vector3D aCBoxCenter) {
            var info = getInfo(aCBoxCenter);
            if (sender == info.Reserver) {
                info.Reserved =
                info.Reserver = 0;
                setInfo(info);
            }
        }
    }
    struct BoxInfo {
        public long Reserver;
        public double Reserved;
        public bool Obstructed;
        public Vector3D Position;
    }
}
