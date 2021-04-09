using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    
    
    delegate Vector3D VectorHandler();
    
    
    public delegate void UpdateHandler(double time);

    delegate void updateHandler();
    class BaseThatUpdates<T> {
        protected Func<T> onUpdate;
        public T Update() => onUpdate == null ? default(T) : onUpdate();
    }
    class MyThing : BaseThatUpdates<bool> {
        MyThing() {
            onUpdate = stepOne;
        }
        bool stepOne() {
            onUpdate = stepTwo;
            return true;
        }
        bool stepTwo() {
            return false;
        }
    }
}
