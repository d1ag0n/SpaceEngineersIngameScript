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
            onUpdate = UpdateAction;
        }

        void UpdateAction() {
            var cbox = BOX.GetCBox(Volume.Center);

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
        public void Reserve() {
            if ((MAF.Now - reserveRequest).TotalSeconds > reserveInterval) {
                mManager.mProgram.IGC.SendUnicastMessage(controller.MotherId, "Dock", Dock.Box());
                reserveRequest = MAF.Now;
            }
        }
        public void Reserve(BoxInfo b) {
            
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
