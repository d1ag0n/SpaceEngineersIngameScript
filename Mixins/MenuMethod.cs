using System;

namespace IngameScript {
    public struct MenuMethod {
        public readonly string Name;
        public readonly object State;
        public readonly Func<MenuModule, object, Menu> Method;
        
        public MenuMethod(string aName, object state, Func<MenuModule, object, Menu> aMethod) {
            Name = aName;
            State = state;
            Method = aMethod;
        }

        public MenuMethod(string aName, Action aAction) {
            Name = aName;
            State = null;
            Method = (a, b) => { aAction(); return null; };
        }
    }
}
