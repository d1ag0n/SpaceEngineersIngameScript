using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage;
using VRageMath;
using System;
namespace IngameScript {
    class ATCLientModule : Module<IMyShipConnector> {
        const double reserveInterval = 1.0;
        DateTime reserveRequest;
        public DockMsg Dock;

        public bool connected => Connector.Status == MyShipConnectorStatus.Connected;
        public IMyShipConnector Connector {
            get;private set;
        }
        readonly Dictionary<int, BoxInfo> mBoxes = new Dictionary<int, BoxInfo>();

        public override bool Accept(IMyTerminalBlock aBlock) {
            if (Connector == null) {
                if (base.Accept(aBlock)) {
                    Connector = aBlock as IMyShipConnector;
                    return true;
                }
            }
            return false;
        }

        public ATCLientModule(ModuleManager aManager) : base(aManager) {
            aManager.mIGC.SubscribeUnicast("ATC", onATCMessage);
            aManager.mIGC.SubscribeUnicast("Dock", onDockMessage);
            if (aManager.Drill) {
                aManager.mIGC.SubscribeUnicast("Drill", onDrillMessage);
                
            }
            aManager.mIGC.SubscribeUnicast("Cancel", onCancelMessage);
            onUpdate = UpdateAction;
            lastRegistration = -60d;
        }
        double lastRegistration;
        void UpdateAction() {
            var cbox = BOX.GetCBox(Volume.Center);
            var dif = mManager.Runtime - lastRegistration;
            controller.logger.log($"controller.OnMission={controller.OnMission}");
            
            if (mManager.Drill && !controller.OnMission && dif > 60d) {
                if (controller.MotherId != 0) {
                    if (mManager.mProgram.IGC.SendUnicastMessage(controller.MotherId, "Registration", "Drill")) {
                        lastRegistration = mManager.Runtime;
                    }
                }
            }

        }
        void onCancelMessage(IGC.Envelope e) => controller.CancelMission();
        void onDrillMessage(IGC.Envelope e) {
            var ore = ThyDetectedEntityInfo.Ore.Unbox(e.Message.Data);
            var m = new DrillMission(controller, ore.Item2, ore.Item1);
            controller.NewMission(m);
        }
        void onDockMessage(IGC.Envelope e) {
            Dock = DockMsg.Unbox(e.Message.Data);
            Dock.Reserved = MAF.Now;
        }
        void onATCMessage(IGC.Envelope e) {
            var msg = ATCMsg.Unbox(e.Message.Data);
            var c = BOX.GetCBox(msg.Info.Position);
            var i = BOX.CVectorToIndex(c.Center);
            logger.persist("Incoming ATC message " + msg.Subject);
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
                var result = mManager.mProgram.IGC.SendUnicastMessage(controller.MotherId, "Dock", Dock.Box());
                reserveRequest = MAF.Now;
            }
        }
        public void ReserveCBox(BoxInfo b) {
            
            if ((MAF.Now - reserveRequest).TotalSeconds > reserveInterval) {
                var msg = new ATCMsg();
                msg.Info = b;
                msg.Subject = enATC.Reserve;
                var result = mManager.mProgram.IGC.SendUnicastMessage(controller.MotherId, "ATC", msg.Box());
                reserveRequest = MAF.Now;
                logger.log("Reservation send result ", result);
            } else {
                logger.log("Waiting to send reservation.");
            }
            
        }

        public BoxInfo GetBoxInfo(Vector3D aPos) {
            var b = BOX.GetCBox(aPos);
            logger.log(b);
            var i = BOX.CVectorToIndex(b.Center);
            BoxInfo result;
            if (!mBoxes.TryGetValue(i, out result)) {
                result.Position = b.Center;
            }
            return result;
            
        }
    }
}
