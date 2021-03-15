using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    public interface zIAccept {
        bool Accept(IMyTerminalBlock aBlock);
        bool Remove(IMyTerminalBlock aBlock);
    }
}
