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
        List<MyInventoryItem> list = new List<MyInventoryItem>();
        IMyCargoContainer cargo;
        public Program() {
            cargo = GridTerminalSystem.GetBlockWithName("Cargo - Reactor 1") as IMyCargoContainer;
        }

        public void Main(string argument, UpdateType updateSource) {
            Echo("running");
            var inv = cargo.GetInventory();
            
            inv.GetItems(list);

            foreach (var item in list) {
                
                var info = item.Type.GetItemInfo();
                var amount = (float)item.Amount;
                var result = inv.TransferItemTo(inv, item);
                Echo($"result={result}");
            }
            if (list.Count == 0) {
                Echo("no items");
            }
            list.Clear();
        }
    }
}
