using System;

namespace IngameScript {
    public class MenuMethod {
        public string Name;
        public readonly object State;
        public readonly Func<MenuModule, object, Menu> Method;
        
        public MenuMethod(string aName, object state, Func<MenuModule, object, Menu> aMethod) {
            Name = aName;
            State = state;
            Method = aMethod;
        }
    }
}
