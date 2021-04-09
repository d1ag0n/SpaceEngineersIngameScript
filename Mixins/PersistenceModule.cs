using System.Text;
using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using System.Xml.Serialization;

namespace IngameScript {
    public class PersistenceModule : Module<IMyShipController> {
        public PersistenceModule(ModuleManager aManager) : base(aManager) {

        }
    }
}
