using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public abstract class ModuleBase {
        public string MenuName;
        public readonly List<MenuMethod> MenuMethods = new List<MenuMethod>();
        public readonly LogModule logger;
        public readonly ShipControllerModule controller;
        public bool Active { get; protected set; }
        protected bool Okay = false;
        public ModuleBase() {
            ModuleManager.GetModule(out logger);
            ModuleManager.GetModule(out controller);
        }
        public void MenuData(List<object> aList) {
             
        }
       
    }
}
