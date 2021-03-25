using Sandbox.ModAPI.Ingame;
using VRage;
using VRageMath;

namespace IngameScript
{
    class ATC : Module<IMyTerminalBlock>
    {
        readonly BoxMap map = new BoxMap();


        public override bool Accept(IMyTerminalBlock aBlock) => false;

        public ATC() {
            ModuleManager.IGCSubscribe("ATC", processMessage);
        }
        public void processMessage(MyIGCMessage aMessage) {
            var atc = ATCMsg.Unbox(aMessage.Data);
            switch (atc.Subject) {
                case enATC.DropReservtion:
                    atc.Info = map.dropReservation(aMessage.Source, atc.Info.Position);
                    break;
                case enATC.Reserve:
                    atc.Info = map.setReservation(aMessage.Source, atc.Info.Position);
                    break;
            }
            ModuleManager.Program.IGC.SendUnicastMessage(aMessage.Source, "ATC", atc.Box());
        }
        
    }

}
