using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    public abstract class ModuleBase {

        abstract public bool Accept(IMyTerminalBlock aBlock);
        abstract public bool Remove(IMyTerminalBlock aBlock);

        public delegate void delSave(Serialize s);
        public delegate void delLoad(Serialize s, string aData);
        
        public string MenuName;
        public readonly LogModule logger;
        public readonly ShipControllerModule controller;
        /// <summary>
        /// MenuMethod list
        /// </summary>
        /// <param name="aPage"></param>
        /// <returns></returns>
        public Menu.Paginator Menu { get; protected set; }
        public Action Update { get; protected set; }
        public delSave Save { get; protected set; }
        public delLoad Load { get; protected set; }
        

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
