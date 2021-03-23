using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    public abstract class MissionBase {
        public bool Complete { get; protected set; }
        protected readonly ShipControllerModule ctr;
        public MissionBase(ShipControllerModule aController) {
            ctr = aController;
        }
        public abstract void Update();
    }
}
