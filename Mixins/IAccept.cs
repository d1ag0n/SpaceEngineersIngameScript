using Sandbox.ModAPI.Ingame;

namespace IngameScript {
    public interface IAccept {
        
        bool Accept(IMyTerminalBlock b);
        bool Remove(IMyTerminalBlock b);
    }
}
