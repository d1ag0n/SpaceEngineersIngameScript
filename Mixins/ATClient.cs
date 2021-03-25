using Sandbox.ModAPI.Ingame;
using VRage;
using VRageMath;

namespace IngameScript {
    class ATCLientModule : Module<IMyTerminalBlock> {
        public override bool Accept(IMyTerminalBlock aBlock) => false;

    }
}
