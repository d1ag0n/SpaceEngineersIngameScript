using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage;
using VRageMath;

namespace IngameScript
{
    class ATCModule : Module<IMyShipConnector>
    {
        readonly HashSet<long> mDrills = new HashSet<long>();
        readonly BoxMap map = new BoxMap();
        readonly List<Connector> mConnectors = new List<Connector>();
        readonly Dictionary<Vector3L, long> mDrillMissions = new Dictionary<Vector3L, long>();
        readonly GridComModule mCom;

        public ATCModule(ModuleManager aManager) : base(aManager) {
            aManager.GetModule(out mCom);
            mCom.SubscribeUnicast("ATC", atcMessage);
            mCom.SubscribeUnicast("Dock", dockMessage);
            mCom.SubscribeUnicast("Registration", registrationMessage);
            onUpdate = UpdateAction;
        }
        void UpdateAction() {
            foreach (var c in mConnectors) {
                c.Update();
            }
        }

        public void CancelDrill(Vector3L aPos) {
            long id;
            if (mDrillMissions.TryGetValue(aPos, out id)) {
                if (mManager.mProgram.IGC.SendUnicastMessage(id, "Cancel", 0)) {
                    mDrillMissions.Remove(aPos);
                }
            }
        }

        public bool SendDrill(ThyDetectedEntityInfo.Ore ore) {
            mLog.persist($"Drill count = {mDrills.Count}");
            if (mDrills.Count > 0) {
                var id = mDrills.FirstElement();
                long existing;
                if (mDrillMissions.TryGetValue(ore.Index, out existing)) {
                    if (existing != id) {
                        return false;
                    }
                } else {
                    mDrillMissions.Add(ore.Index, id);
                }

                if (mManager.mProgram.IGC.SendUnicastMessage(id, "Drill", ore.Box())) {
                    mDrills.Remove(id);
                    
                    return true;
                }
            }
            return false;
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = false;
            if (aBlock.CustomData.Contains("#dock")) {
                result = base.Accept(aBlock);
                if (result) {
                    mConnectors.Add(new Connector(this, aBlock as IMyShipConnector));
                }
            }
            return result;
        }

        void atcMessage(Envelope e) {
            var src = e.Message.Source;
            mLog.persist("Received ATC message from " + src);
            
            var msg = ATCMsg.Unbox(e.Message.Data);
            switch (msg.Subject) {
                case enATC.Drop:
                    msg.Info = map.dropReservation(src, msg.Info.Position);
                    break;
                case enATC.Reserve:
                    msg.Info = map.setReservation(src, msg.Info.Position);
                    break;
            }
            var result = mManager.mProgram.IGC.SendUnicastMessage(src, "ATC", msg.Box());
            mLog.persist("Respose result " + result);
        }
        void registrationMessage(Envelope e) {
            if (e.Message.Data != null) {
                var data = e.Message.Data.ToString();
                if (data == "Drill") {
                    mDrills.Add(e.Message.Source);
                }
            }
        }
        void dockMessage(Envelope e) {
            Connector unReserved = null;
            foreach (var c in mConnectors) {
                if (c.ReservedBy == e.Message.Source) {
                    unReserved = c;
                    break;
                } else if (!c.Reserved) { 
                    unReserved = c;
                }
            }
            if (unReserved != null) {
                doReserveDock(unReserved, e.Message);
            }
        }
        void doReserveDock(Connector c, MyIGCMessage m) {
            var msg = DockMsg.Unbox(m.Data);
            msg.theConnector = c.Dock.Position;
            msg.ConnectorFace = c.Dock.Orientation.Forward;
            if (mManager.mProgram.IGC.SendUnicastMessage(m.Source, "Dock", msg.Box())) {
                c.Reserved = true;
                c.ReservedBy = m.Source;

            }
        }
    }

    struct DockMsg {
        public Vector3I theConnector;
        public Base6Directions.Direction ConnectorFace;
        public DateTime Reserved;
        public Vector3D ConnectorDir => Base6Directions.GetVector(ConnectorFace);

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
        
        public MyTuple<int, MyTuple<long, long, bool, Vector3D>>  Box() => MyTuple.Create((int)Subject, Info.Box());
        public static ATCMsg Unbox(object data) {
            var t = (MyTuple<int, object>)data;
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
