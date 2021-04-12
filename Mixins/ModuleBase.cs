using Sandbox.ModAPI.Ingame;
using System;
using System.Collections;

namespace IngameScript {
    public abstract class ModuleBase {

        protected bool Okay = false;

        public virtual bool Accept(IMyTerminalBlock aBlock) => false;
        public virtual void Remove(IMyTerminalBlock aBlock) { }

        public readonly ModuleManager mManager;
        public readonly LogModule mLog;
        public readonly ShipControllerModule mController;

        public bool Active;
        public Action<double> onIGC { get; protected set; }
        public Action onUpdate { get; protected set; }
        public Action<string> onInput { get; protected set; }
        public Func<string> onSave { get; protected set; }
        public Action<string> onLoad { get; protected set; }

        public ModuleBase(ModuleManager aManager) {
            mManager = aManager;
            mLog = aManager.mLog;
            mController = aManager.mController;
            mManager.Add(this);
        }
        /*public void MenuData(List<object> aList) {
             
        }*/
       
    }
}
