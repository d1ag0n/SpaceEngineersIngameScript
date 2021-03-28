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
        readonly List<Connector> mConnectors = new List<Connector>();

        public ATCModule() {
            ModuleManager.IGCSubscribe("ATC", atcMessage);
            ModuleManager.IGCSubscribe("Dock", dockMessage);
            onUpdate = UpdateAction;
        }
        void UpdateAction() {
            foreach (var c in mConnectors) {
                c.Update();
            }
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = base.Accept(aBlock);
            if (result) {
                mConnectors.Add(new Connector(aBlock as IMyShipConnector));
            }
            return result;
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
            foreach (var c in mConnectors) {
                if (!c.Reserved) {
                    msg.theConnector = c.Dock.Position;
                    msg.ConnectorFace = c.Dock.Orientation.Forward;
                    if (ModuleManager.Program.IGC.SendUnicastMessage(m.Source, "Dock", msg.Box())) {
                        c.Reserved = true;
                    }
                    return;
                }
            }
        }
    }

    struct DockMsg {
        public Vector3I theConnector;
        public Base6Directions.Direction ConnectorFace;
        public DateTime Reserved;
        public bool isReserved => (MAF.Now - Reserved).TotalMinutes < Connector.reserveTime;
        public static DockMsg Unbox(object data) {
            var msg = (MyTuple<Vector3I, int>)data;
            var result = new DockMsg();
            result.theConnector = msg.Item1;
            result.ConnectorFace = (Base6Directions.Direction)msg.Item2;
            return result;
        }
        public MyTuple<Vector3I, int> Box() =>
            MyTuple.Create(theConnector, (int)ConnectorFace);
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
