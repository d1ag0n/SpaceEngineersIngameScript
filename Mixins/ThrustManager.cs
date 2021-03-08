using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript {
    class ThrustManager : Module<IMyThrust> {

        readonly Logger g;
        readonly List<Thrust> mThrust = new List<Thrust>();
        
        public ThrustManager() {
            GetModule(out g);
        }


        public override bool Accept(IMyTerminalBlock b) {
            bool result = false;
            if (base.Accept(b)) {
                var t = b as IMyThrust;
                mThrust.Add(new Thrust(t, getThrustType(t)));
                result = true;
            }
            return result;
        }
        public override bool Remove(IMyTerminalBlock b) {
            var result = base.Remove(b);
            if (result) {
                for (int i =0; i < mThrust.Count; i++) {
                    if (mThrust[i].mEngine.EntityId == b.EntityId) {
                        mThrust.RemoveAt(i);
                        break;
                    }
                }
            }
            return result;
        }

        Thrust.Group getThrustType(IMyThrust aThrust) {
            switch(aThrust.BlockDefinition.SubtypeName) {
                case "LargeBlockLargeHydrogenThrust":
                    return Thrust.Group.Fuel;
                case "LargeBlockLargeThrust":
                    return Thrust.Group.Ion;
                case "LargeBlockLargeAtmosphericThrust":
                    return Thrust.Group.Air;
                default:
                    g.persist($"{aThrust.BlockDefinition.SubtypeName}");
                    return Thrust.Group.Not;
            }
        }
        public void Update() {
            g.log("Thruster count=", mThrust.Count);
            foreach (var t in mThrust) {
                g.log(t.mGroup);
            }

        }
        public IMyThrust Get(int aIndex) {
            IMyThrust result = null;
            if (-1 < aIndex && mThrust.Count > aIndex) {
                result = mThrust[aIndex].mEngine;
            }
            return result;
        }
    }
}
