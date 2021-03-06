using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage;
using VRageMath;
using System;
namespace IngameScript {
    class ATClientModule : Module<IMyShipConnector> {
        
        const double reserveInterval = 1.0;
        readonly ThrustModule mThrust;
        readonly ShipControllerModule mController;
        readonly GridComModule mCom;
        readonly Dictionary<int, BoxInfo> mBoxes = new Dictionary<int, BoxInfo>();
        readonly List<IMyBatteryBlock> mBatteries = new List<IMyBatteryBlock>();

        DateTime reserveRequest;
        
        public DockMessage Dock;

        public bool connected => mConnector.Status == MyShipConnectorStatus.Connected;
        public readonly MotherState Mother;
        public IMyShipConnector mConnector { get; private set; }



        public ATClientModule(ModuleManager aManager) : base(aManager) {
            Mother = new MotherState(aManager);
            aManager.GetModule(out mController);
            aManager.GetModule(out mCom);
            aManager.GetModule(out mThrust);
            Active = true;
            onUpdate = initAction;
            
            lastRegistration = -60d;

            mCom.SubscribeBroadcast("MotherState", Mother.Update);
            mCom.SubscribeUnicast("ATC", onATCMessage);
            mCom.SubscribeUnicast("Dock", onDockMessage);
            mCom.SubscribeUnicast("Cancel", onCancelMessage);
            if (aManager.Drill) {
                mCom.SubscribeUnicast("Drill", onDrillMessage);
            }
            
        }
        public void Connect() {
            mConnector.Enabled = true;
            mConnector.Connect();
            if (connected) {
                setBatteries(ChargeMode.Recharge);
            }
        }
        public void Disconnect() {
            setBatteries(ChargeMode.Discharge);
            mConnector.Disconnect();
            if (connected) {
                setBatteries(ChargeMode.Recharge);
            } else {
                mConnector.Enabled = false;
            }
        }
        void initAction() {
            setBatteries(connected ? ChargeMode.Recharge : ChargeMode.Discharge);
            onUpdate = UpdateAction;
            
        }
        void setBatteries(ChargeMode aMode) {
            foreach (var b in mBatteries) {
                b.ChargeMode = aMode;
            }
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            var b = aBlock as IMyBatteryBlock;
            if (b != null) {
                if (mRegistry.Add(b.EntityId)) {
                    mBatteries.Add(b);
                }
            }
            if (mConnector == null) {
                if (base.Accept(aBlock)) {
                    mConnector = aBlock as IMyShipConnector;
                    return true;
                }
            }
            return false;
        }

        double lastRegistration;
        void UpdateAction() {
            var cbox = BOX.GetCBox(Volume.Center);
            var dif = mManager.Runtime - lastRegistration;
            //mLog.log($"controller.OnMission={mController.OnMission}, mThrust.Damp={mThrust.Damp}");

            if (mManager.Drill && !mController.OnMission && dif > 60d) {
                if (Mother.Id != 0) {
                    if (mManager.mProgram.IGC.SendUnicastMessage(Mother.Id, "Registration", $"Drill:{Grid.CustomName}")) {
                        lastRegistration = mManager.Runtime;
                        //mLog.persist("Reg sent");
                    } else {
                        //mLog.log("IGC fail");
                    }
                } else {
                    //mLog.log("No mother");
                }
            } else {
                //mLog.log($"Not sent yet - drill={mManager.Drill}, dif={dif}");
            }
            //mLog.log($"ATClient Updated - lastRegistration={lastRegistration}");
        }
        void onCancelMessage(Envelope e) {
            mController.CancelMission();
            if (!connected) {
                mController.NewMission(new DockMission(mManager));
            }
        }
        void onDrillMessage(Envelope e) {
            var ore = Ore.Unpack(e.Message.Data);
            var m = new DrillMission(mManager, ore.Item3, ore.Item1, ore.Item2);
            mController.NewMission(m);
        }
        void onDockMessage(Envelope e) {
            Dock = DockMessage.Unbox(e.Message.Data);
            Dock.Reserved = MAF.Now;
        }
        void onATCMessage(Envelope e) {
            var msg = ATCMessage.Unbox(e.Message.Data);
            var c = BOX.GetCBox(msg.Info.Position);
            var i = BOX.CVectorToIndex(c.Center);
            mLog.persist("Incoming ATC message " + msg.Subject);
            switch (msg.Subject) {
                case ATCSubject.Reserve:
                    mBoxes[i] = msg.Info;
                    break;
                case ATCSubject.Drop:
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
                var msg = new ATCMessage();
                msg.Info = b;
                msg.Subject = ATCSubject.Reserve;
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
