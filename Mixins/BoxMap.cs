﻿using System;
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
        string key(Vector3D v) => $"{v.X:f0}.{v.Y:f0}.{v.Z:f0}";
        Dictionary<int, BoxInfo> getKMap(Vector3D aPosition, out Vector3D aKBox) {

            Dictionary<int, BoxInfo> result;

            aKBox = BOX.WorldToK(aPosition);
            var kkey = key(aKBox);

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
    
}
