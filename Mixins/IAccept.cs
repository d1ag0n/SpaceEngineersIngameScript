using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    interface IAccept {
        bool Accept(IMyTerminalBlock b);
        bool Remove(IMyTerminalBlock b);
    }
}
