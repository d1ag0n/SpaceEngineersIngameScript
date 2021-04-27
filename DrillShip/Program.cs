using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public partial class Program : MyGridProgram {
        readonly ModuleManager mManager;
        public Program() {
            mManager = new ModuleManager(this, "Drill Ship", "logConsole", 1);
            mManager.Drill = true;
            new ShipControllerModule(mManager);
            new GridComModule(mManager);
            var g = new GyroModule(mManager);
            var t = new ThrustModule(mManager);
            new CameraModule(mManager);
            new OreDetectorModule(mManager);
            var c = new ATClientModule(mManager);
            mManager.Initialize();
            var list = new List<IMyShipDrill>();
            mManager.getByType(list);
            foreach (var d in list) {
                d.Enabled = false;
            }
            if (c.connected) {
                t.Damp = false;
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        public void Main(string arg, UpdateType aType) => mManager.Update(arg, aType);
    }
}
