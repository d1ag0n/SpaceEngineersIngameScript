using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRageMath;

namespace IngameScript {

    // todo merge this class
    class Connector {
        public const double reserveTime = 1.0;
        public readonly IMyShipConnector Dock;
        bool _Reserved;
        public long ReservedBy;
        DateTime ReserveSet;
        readonly ATCModule mATC;
        public bool Reserved {
            get {
                return _Reserved;
            }
            set {
                if (_Reserved != value) {
                    _Reserved = value;
                }
                ReserveSet = MAF.Now;
            }
        }
        public Connector(ATCModule aATC, IMyShipConnector aDock) {
            mATC = aATC;
            Dock = aDock;
            Reserved = connected;
        }
        bool connected => Dock.Status == MyShipConnectorStatus.Connected;
        public void Update() {
            if (connected) {
                Reserved = true;
            } else {
                if (Reserved && (MAF.Now - ReserveSet).TotalMinutes > reserveTime) {
                    Reserved = false;
                }
            }
        }
    }
}
