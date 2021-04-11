
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage;
using VRageMath;

namespace IngameScript
{
    struct ATCMessage {
        public ATCSubject Subject;
        public BoxInfo Info;
        
        public MyTuple<int, MyTuple<long, long, bool, Vector3D>>  Box() => MyTuple.Create((int)Subject, Info.Box());
        public static ATCMessage Unbox(object data) {
            var t = (MyTuple<int, object>)data;
            var result = new ATCMessage();
            result.Subject = (ATCSubject)t.Item1;
            result.Info = BoxInfo.Unbox(t.Item2);
            return result;
        }
    }
	
}
