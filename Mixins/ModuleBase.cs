using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace IngameScript {
    public abstract class ModuleBase {
        public string MenuName;
        /// <summary>
        /// MenuMethod list
        /// </summary>
        /// <param name="aPage"></param>
        /// <returns></returns>
        public virtual List<object> MenuMethods(int aPage) => null;
        public Action Update { get; protected set; }
        public readonly LogModule logger;
        public readonly ShipControllerModule controller;
        public bool Active { get; protected set; }
        protected bool Okay = false;
        public ModuleBase() {
            ModuleManager.GetModule(out logger);
            ModuleManager.GetModule(out controller);
        }
        /*public void MenuData(List<object> aList) {
             
        }*/
       
    }
}
