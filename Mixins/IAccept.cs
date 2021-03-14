using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    public interface IAccept {
        bool Accept(IMyTerminalBlock aBlock);
        bool Remove(IMyTerminalBlock aBlock);
    }
}
