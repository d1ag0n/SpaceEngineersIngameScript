using System;

namespace IngameScript {
    public struct MenuItem {
        public readonly string Name;
        public readonly object State;
        public readonly Func<MenuModule, object, Menu> Method;
        
        public MenuItem(string aName, object state = null,  Func<MenuModule, object, Menu> aMethod = null) {
            Name = aName;
            State = state;
            Method = aMethod;
        }

        public MenuItem(string aName, Action aAction) {
            Name = aName;
            State = null;
            Method = (a, b) => { aAction(); return null; };
        }
        public MenuItem(string aName) {
            Name = aName;
            State = null;
            Method = null;
        }
        public MenuItem(ModuleBase aModule) {
            Name = aModule.MenuName;
            State = aModule;
            Method = null;
        }
    }
}
