using System;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public abstract class ModuleBase {

        abstract public bool Accept(IMyCubeBlock aBlock);
        abstract public bool Remove(IMyCubeBlock aBlock);

        
        
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
            ModuleManager.GetModule(out controller);
        }
        /*public void MenuData(List<object> aList) {
             
        }*/
       
    }
}
