using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    class PeriscopeModule : Module<IMyMotorStator> {
        IMyMotorStator turn, point;

        public static new PeriscopeModule Factory() {
            return new PeriscopeModule();
        }
        public override bool Accept(IMyTerminalBlock b) {
            var result = b.CustomName.Contains("#periscope");
            if (turn == null) {

            } else {

            }
            return base.Accept(b);
        }
    }
}