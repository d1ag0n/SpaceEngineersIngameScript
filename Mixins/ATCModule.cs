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
        readonly Dictionary<Vector3L, DrillMission> mDrillMissions = new Dictionary<Vector3L, DrillMission>();
        readonly GridComModule mCom;
        readonly Dictionary<long, string> mDrillNames = new Dictionary<long, string>();

        public ATCModule(ModuleManager aManager) : base(aManager) {
            aManager.GetModule(out mCom);
            mCom.SubscribeUnicast("ATC", atcMessage);
            mCom.SubscribeUnicast("Dock", dockMessage);
            mCom.SubscribeUnicast("Registration", registrationMessage);
            onUpdate = UpdateAction;
            Active = true;
        }
        void UpdateAction() {
            foreach (var c in mConnectors) {
                c.Update();
            }
        }

        public void CancelDrill(Vector3L aPos) {
            DrillMission m;
            if (mDrillMissions.TryGetValue(aPos, out m)) {
                if (mManager.mProgram.IGC.SendUnicastMessage(m.Id, "Cancel", 0)) {
                    mDrillMissions.Remove(aPos);
                }
            }
        }
        public struct DrillMission {
            public readonly long Id;
            public readonly string Name;
            public readonly Vector3L Location;
            public DrillMission(long aId, string aName, Vector3L aLocation) {
                Id = aId;
                Name = aName;
                Location = aLocation;
            }
        }
        public void DrillMissions(List<DrillMission> aList) {
            foreach (var dm in mDrillMissions) {
                aList.Add(dm.Value);
            }
        }

        public bool SendDrill(Ore ore) {
            if (mDrills.Count > 0) {
                var id = mDrills.FirstElement();
                DrillMission existing;
                if (mDrillMissions.TryGetValue(ore.Index, out existing)) {
                    if (existing.Id != id) {
                        return false;
                    }
                }

                if (mManager.mProgram.IGC.SendUnicastMessage(id, "Drill", ore.Pack())) {
                    mDrills.Remove(id);

                    mDrillMissions.Add(ore.Index, new DrillMission(id, mDrillNames[id], ore.Index));
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
                    mConnectors.Add(new Connector(aBlock as IMyShipConnector));
                }
            }
            return result;
        }

        void atcMessage(Envelope e) {
            var src = e.Message.Source;
            mLog.persist("Received ATC message from " + src);
            
            var msg = ATCMessage.Unbox(e.Message.Data);
            switch (msg.Subject) {
                case ATCSubject.Drop:
                    msg.Info = map.dropReservation(src, msg.Info.Position);
                    break;
                case ATCSubject.Reserve:
                    msg.Info = map.setReservation(src, msg.Info.Position);
                    break;
            }
            var result = mManager.mProgram.IGC.SendUnicastMessage(src, "ATC", msg.Box());
            mLog.persist("Respose result " + result);
        }
        void registrationMessage(Envelope e) {
            if (e.Message.Data != null) {
                var data = e.Message.Data.ToString();
                if (data.StartsWith("Drill:")) {
                    mDrillNames[e.Message.Source] = data.Substring(6);
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
            var msg = DockMessage.Unbox(m.Data);
            msg.theConnector = c.Dock.Position;
            msg.ConnectorFace = c.Dock.Orientation.Forward;
            if (mManager.mProgram.IGC.SendUnicastMessage(m.Source, "Dock", msg.Box())) {
                c.Reserved = true;
                c.ReservedBy = m.Source;

            }
        }
    }

    

    
}
