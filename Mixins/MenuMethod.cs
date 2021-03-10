using System;

namespace IngameScript {
    public class MenuMethod {
        public string Name;
        public readonly Action<object> Method;

        public MenuMethod(string aName, Action<object> aMethod) {
            Name = aName;
            Method = aMethod;
        }
    }
}
