using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    public abstract class ModuleBase {

        abstract public bool Accept(IMyTerminalBlock aBlock);
        abstract public bool Remove(IMyTerminalBlock aBlock);

        
        
        public string MenuName;
        public readonly LogModule logger;
        public readonly ShipControllerModule controller;
        /// <summary>
        /// MenuMethod list
        /// </summary>
        /// <param name="aPage"></param>
        /// <returns></returns>
        public PaginationHandler onPage { get; protected set; }
        public Action onUpdate { get; protected set; }
        public SaveHandler onSave { get; protected set; }
        public LoadHandler onLoad{ get; protected set; }
        

        public bool Active { get; protected set; }
        protected bool Okay = false;
        public ModuleBase() {
            ModuleManager.GetModule(out logger);
            if (this is ShipControllerModule) {
                controller = this as ShipControllerModule;
            } else {
                ModuleManager.GetModule(out controller);
            }
        }
        /*public void MenuData(List<object> aList) {
             
        }*/
       
    }
}
