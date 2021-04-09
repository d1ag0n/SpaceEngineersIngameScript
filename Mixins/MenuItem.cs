using System;

namespace IngameScript {
    public struct MenuItem : IMenuItem {
        public readonly string mName;
        readonly Func<Menu> mMethod;
        public static MenuItem CreateItem(string aName, Action aMethod) => new MenuItem(aName, () => { aMethod(); return null; });
        public MenuItem(string aName, Func<Menu> aMethod = null) {
            mName = aName;
            mMethod = aMethod;
        }
        public Menu Run() => mMethod == null ? null : mMethod();
    }
}
