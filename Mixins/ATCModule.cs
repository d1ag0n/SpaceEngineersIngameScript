using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript
{
    class ATCModule : Module<IMyTerminalBlock>
    {
        readonly BoxMap map = new BoxMap();

        public ATCModule() {
            ModuleManager.IGCSubscribe("ATC", processMessage);
        }
        public override bool Accept(IMyTerminalBlock aBlock) => false;

        void processMessage(MyIGCMessage m) {
            var msg = ATCMsg.Unbox(m.Data);
            switch (msg.Subject) {
                case enATC.Drop:
                    msg.Info = map.dropReservation(m.Source, msg.Info.Position);
                    break;
                case enATC.Reserve:
                    msg.Info = map.setReservation(m.Source, msg.Info.Position);
                    break;
            }
            ModuleManager.Program.IGC.SendUnicastMessage(m.Source, "ATC", msg.Box());
        }
    }
    struct ATCMsg {
        public enATC Subject;
        public BoxInfo Info;

        public MyTuple<int, MyTuple<long, long, bool, Vector3D>> Box() => MyTuple.Create((int)Subject, Info.Box());
        public static ATCMsg Unbox(object data) {
            var t = (MyTuple<int, MyTuple<long, long, bool, Vector3D>>)data;
            ATCMsg result;
            result.Subject = (enATC)t.Item1;
            result.Info = BoxInfo.Unbox(t.Item2);
            return result;
        }
    }
    enum enATC
    {
        Info,
        Reserve,
        Drop
    }
}
