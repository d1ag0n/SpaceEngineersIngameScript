using System;
using System.Collections.Generic;

namespace IngameScript {
    public abstract class ModuleBase {
        protected readonly Action Void = () => { };
        public delegate void delSave(Serialize s);
        public delegate void delLoad(Serialize s, string aData);
        public delegate List<object> delMenu(int page);
        public string MenuName;
        public readonly LogModule logger;
        public readonly ShipControllerModule controller;
        /// <summary>
        /// MenuMethod list
        /// </summary>
        /// <param name="aPage"></param>
        /// <returns></returns>
        public delMenu Menu { get; protected set; }
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
