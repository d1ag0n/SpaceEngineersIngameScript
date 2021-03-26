using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript
{
    class ATCModule : Module<IMyShipConnector>
    {
        readonly BoxMap map = new BoxMap();


        public ATCModule() {
            ModuleManager.IGCSubscribe("ATC", atcMessage);
            ModuleManager.IGCSubscribe("Dock", dockMessage);
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (base.Accept(aBlock)) {
                var c = aBlock as IMyShipConnector;

            }
        }

        void atcMessage(MyIGCMessage m) {
            logger.persist("Received ATC message from " + m.Source);
            
            var msg = ATCMsg.Unbox(m.Data);
            switch (msg.Subject) {
                case enATC.Drop:
                    msg.Info = map.dropReservation(m.Source, msg.Info.Position);
                    break;
                case enATC.Reserve:
                    msg.Info = map.setReservation(m.Source, msg.Info.Position);
                    break;
            }
            var result = ModuleManager.Program.IGC.SendUnicastMessage(m.Source, "ATC", msg.Box());
            logger.persist("Respose result " + result);
        }
        void dockMessage(MyIGCMessage m) {
            var msg = DockMsg.Unbox(m.Data);

        }
    }
    struct DockMsg {
        public Vector3I Connector;
        public Base6Directions.Direction ConnectorFace;
        public static DockMsg Unbox(object data) {
            var msg = (MyTuple<Vector3I, int>)data;
            var result = new DockMsg();
            result.Connector = msg.Item1;
            result.ConnectorFace = (Base6Directions.Direction)msg.Item2;
            return result;
        }
        public MyTuple<Vector3I, int> Box() =>
            MyTuple.Create(Connector, (int)ConnectorFace);
    }
    struct ATCMsg {
        public enATC Subject;
        public BoxInfo Info;
        
        public MyTuple<int, MyTuple<long, long, bool, Vector3D>> Box() => MyTuple.Create((int)Subject, Info.Box());
        public static ATCMsg Unbox(object data) {
            var t = (MyTuple<int, MyTuple<long, long, bool, Vector3D>>)data;
            var result = new ATCMsg();
            result.Subject = (enATC)t.Item1;
            result.Info = BoxInfo.Unbox(t.Item2);
            result.Connector = 
            return result;
        }
    }
    enum enATC
    {
        Info,
        Reserve,
        Drop,
        Dock
    }
}
