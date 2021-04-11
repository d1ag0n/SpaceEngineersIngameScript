using System;

namespace IngameScript {
    public struct MenuItem {
        public readonly string mName;
        readonly Func<Menu> mMethod;
        readonly Menu mMenu;
        public static MenuItem CreateItem(string aName, Action aMethod, Menu aMenu = null) {

            return new MenuItem(aName, () => { aMethod(); return aMenu; });
        }
        public MenuItem(string aName, Func<Menu> aMethod) {
            mName = aName;
            mMethod = aMethod;
            mMenu = null;
        }
        public MenuItem(string aName, Menu aMenu) {
            mName = aName;
            mMethod = null;
            mMenu = aMenu;
        }
        public Menu Run() {
            if (mMethod == null) {
                return mMenu;
            }
            return mMethod();
        }
    }
}
