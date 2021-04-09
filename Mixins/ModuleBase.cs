using Sandbox.ModAPI.Ingame;
using System;
using System.Collections;

namespace IngameScript {
    public abstract class ModuleBase {

        public virtual bool Accept(IMyTerminalBlock aBlock) => false;
        public virtual void Remove(IMyTerminalBlock aBlock) { }

        public readonly ModuleManager mManager;

        
        public readonly LogModule mLog;
        protected ShipControllerModule mController => mManager.mController;
        /// <summary>
        /// MenuMethod list
        /// </summary>
        /// <param name="aPage"></param>
        /// <returns></returns>
        //public IEnumerable onPage { get; protected set; }
        public Action onUpdate { get; protected set; }
        public SaveHandler onSave { get; protected set; }
        public LoadHandler onLoad{ get; protected set; }
        public Action<string> onInput;

        public bool Active;
        protected bool Okay = false;

        public ModuleBase(ModuleManager aManager) {
            mManager = aManager;
            mManager.GetModule(out mLog);
        }
        /*public void MenuData(List<object> aList) {
             
        }*/
       
    }
}
