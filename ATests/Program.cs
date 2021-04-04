using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        IMyTextPanel lcd;
        IMyShipController rc;
        public Program() {
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(null, b => {
                lcd = b as IMyTextPanel;
                return false;
            });
            GridTerminalSystem.GetBlocksOfType<IMyShipController>(null, b => {
                rc = b as IMyShipController;
                return false;
            });
        }

        public void Save() {
        }
        public string gps(string aName, Vector3D aPos) {
            // GPS:ARC_ABOVE:19680.65:144051.53:-109067.96:#FF75C9F1:
            var sb = new StringBuilder("GPS:");
            sb.Append(aName);
            sb.Append(":");
            sb.Append(aPos.X.ToString("F2"));
            sb.Append(":");
            sb.Append(aPos.Y.ToString("F2"));
            sb.Append(":");
            sb.Append(aPos.Z.ToString("F2"));
            sb.Append(":#FFFF00FF:");
            return sb.ToString();
        }
        public void Main(string argument, UpdateType updateSource) {
            var obb = MAF.obb(Me.CubeGrid, 2.5);

            var sb = new StringBuilder();
            var sm = rc.CalculateShipMass();
            sb.AppendLine($"BaseMass={sm.BaseMass}");
            sb.AppendLine($"PhysicalMass={sm.PhysicalMass}");
            sb.AppendLine($"TotalMass={sm.TotalMass}");
            lcd.WriteText(sb.ToString());

        }
    }
}
