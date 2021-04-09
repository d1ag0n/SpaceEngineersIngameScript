using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage;
using VRageMath;
using System;
namespace IngameScript {
    class ATClientModule : Module<IMyShipConnector> {
        
        const double reserveInterval = 1.0;

        readonly GridComModule mCom;
        readonly Dictionary<int, BoxInfo> mBoxes = new Dictionary<int, BoxInfo>();

        DateTime reserveRequest;
        
        public DockMsg Dock;

        public bool connected => Connector.Status == MyShipConnectorStatus.Connected;
        public readonly MotherState Mother;
        public IMyShipConnector Connector { get; private set; }

        
        void onMotherState(Envelope e) {

        }


        public ATClientModule(ModuleManager aManager) : base(aManager) {
            Mother = new MotherState(aManager);
            //aManager.GetModule(out mController);
            aManager.GetModule(out mCom);

            onUpdate = UpdateAction;
            lastRegistration = -60d;

            mCom.SubscribeBroadcast("MotherState", onMotherState);
            mCom.SubscribeUnicast("ATC", onATCMessage);
            mCom.SubscribeUnicast("Dock", onDockMessage);
            mCom.SubscribeUnicast("Cancel", onCancelMessage);
            if (aManager.Drill) {
                mCom.SubscribeUnicast("Drill", onDrillMessage);
            }
            
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (Connector == null) {
                if (base.Accept(aBlock)) {
                    Connector = aBlock as IMyShipConnector;
                    return true;
                }
            }
            return false;
        }

        double lastRegistration;
        void UpdateAction() {
            var cbox = BOX.GetCBox(Volume.Center);
            var dif = mManager.Runtime - lastRegistration;
            mLog.log($"controller.OnMission={mController.OnMission}");
            
            if (mManager.Drill && !mController.OnMission && dif > 60d) {
                if (Mother.Id != 0) {
                    if (mManager.mProgram.IGC.SendUnicastMessage(Mother.Id, "Registration", "Drill")) {
                        lastRegistration = mManager.Runtime;
                    }
                }
            }

        }
        void onCancelMessage(Envelope e) => mController.CancelMission();
        void onDrillMessage(Envelope e) {
            var ore = Ore.Unbox(e.Message.Data);
            var m = new DrillMission(mManager, ore.Item2, ore.Item1);
            mController.NewMission(m);
        }
        void onDockMessage(Envelope e) {
            Dock = DockMsg.Unbox(e.Message.Data);
            Dock.Reserved = MAF.Now;
        }
        void onATCMessage(Envelope e) {
            var msg = ATCMsg.Unbox(e.Message.Data);
            var c = BOX.GetCBox(msg.Info.Position);
            var i = BOX.CVectorToIndex(c.Center);
            mLog.persist("Incoming ATC message " + msg.Subject);
            switch (msg.Subject) {
                case enATC.Reserve:
                    mBoxes[i] = msg.Info;
                    break;
                case enATC.Drop:
                    mBoxes.Remove(i);
                    break;
            }
        }
        public void ReserveDock() {
            
            if ((MAF.Now - reserveRequest).TotalSeconds > reserveInterval) {
                var result = mManager.mProgram.IGC.SendUnicastMessage(Mother.Id, "Dock", Dock.Box());
                reserveRequest = MAF.Now;
            }
        }
        public void ReserveCBox(BoxInfo b) {
            
            if ((MAF.Now - reserveRequest).TotalSeconds > reserveInterval) {
                var msg = new ATCMsg();
                msg.Info = b;
                msg.Subject = enATC.Reserve;
                var result = mManager.mProgram.IGC.SendUnicastMessage(Mother.Id, "ATC", msg.Box());
                reserveRequest = MAF.Now;
                mLog.log("Reservation send result ", result);
            } else {
                mLog.log("Waiting to send reservation.");
            }
            
        }

        public BoxInfo GetBoxInfo(Vector3D aPos) {
            var b = BOX.GetCBox(aPos);
            mLog.log(b);
            var i = BOX.CVectorToIndex(b.Center);
            BoxInfo result;
            if (!mBoxes.TryGetValue(i, out result)) {
                result.Position = b.Center;
            }
            return result;
            
        }
    }
}
