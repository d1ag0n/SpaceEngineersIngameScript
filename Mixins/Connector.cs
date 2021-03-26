using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript {
    class Connector {
        public readonly IMyShipConnector Dock;
        bool _Reserved;
        DateTime ReserveSet;

        public bool Reserved {
            get {
                return _Reserved;
            }
            set {
                if (_Reserved != value) {
                    CanRelease = false;
                    _Reserved = value;
                    ReserveSet = MAF.Now;
                }
            }
        }
        bool CanRelease;
        public Connector(IMyShipConnector aDock) {
            Dock = aDock;
            Reserved = status();
        }
        bool status() => Dock.Status == MyShipConnectorStatus.Connected;
        public void Update() {
            if (Reserved) {
                var s = status();
                if (CanRelease) {
                    Reserved = s;
                } else {
                    if ((MAF.Now - ReserveSet).TotalMinutes > 5.0) {
                        Reserved = false;
                    } else {
                        CanRelease = s;
                    }
                }
            }
        }
    }
}
