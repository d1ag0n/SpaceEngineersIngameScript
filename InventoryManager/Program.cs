using Library;
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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string tagInventory = "#inventory";
        const string tagStone = "#stone";
        const string tagAnything = "#anything";

        readonly GTS mGTS;

        List<IMyCargoContainer> mCargo;
        

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            mGTS = new GTS(this, tagInventory);
            init();
        }

        void init() {
            mCargo = new List<IMyCargoContainer>();
            mGTS.getByTag(tagInventory, mCargo);
        }

        void reinit() {
            mGTS.init();
            init();
        }

        public void Save() {
            
        }

        public void Main(string argument, UpdateType updateSource) {
            if (updateSource.HasFlag(UpdateType.Terminal)) {
                switch (argument) {
                    case "init":
                        reinit();
                        break;
                }
            }
            if (updateSource.HasFlag(UpdateType.Update100)) {
                
            }
        }
    }
}
