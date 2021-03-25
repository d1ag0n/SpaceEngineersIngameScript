using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage;
using VRageMath;
using System;
namespace IngameScript {
    class ATCLientModule : Module<IMyTerminalBlock> {
        const double reserveInterval = 1.0;
        DateTime reserveRequest;

        readonly Dictionary<int, BoxInfo> mBoxes = new Dictionary<int, BoxInfo>();
        
        public override bool Accept(IMyTerminalBlock aBlock) => false;

        public ATCLientModule() {
            ModuleManager.IGCSubscribe("ATC", onATCMessage);
            onUpdate = UpdateAction;
        }

        void UpdateAction() {
            var cbox = BOX.GetCBox(Volume.Center);

        }
        void onATCMessage(MyIGCMessage m) {
            var msg = ATCMsg.Unbox(m.Data);
            var c = BOX.GetCBox(msg.Info.Position);
            var i = BOX.CVectorToIndex(c.Center);
            switch (msg.Subject) {
                case enATC.Reserve:
                    mBoxes[i] = msg.Info;
                    break;
                case enATC.Drop:
                    mBoxes.Remove(i);
                    break;
            }
        }
        public void Reserve(BoxInfo b) {
            
            if ((MAF.Now - reserveRequest).TotalSeconds > reserveInterval) {
                var msg = new ATCMsg();
                msg.Info = b;
                msg.Subject = enATC.Reserve;
                var result = ModuleManager.Program.IGC.SendUnicastMessage(controller.MotherId, "ATC", msg.Box());
                reserveRequest = MAF.Now;
                logger.log("Reservation send result ", result);
            } else {
                logger.log("Waiting to send reservation.");
            }
            
        }

        public BoxInfo GetBoxInfo(Vector3D aPos) {
            var b = BOX.GetCBox(aPos);
            var i = BOX.CVectorToIndex(b.Center);
            BoxInfo result;
            if (!mBoxes.TryGetValue(i, out result)) {
                result.Position = aPos;
            }
            return result;
            
        }
    }
}
