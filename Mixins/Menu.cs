using System.Text;
using System.Collections.Generic;

namespace IngameScript {
    public class Menu {
        readonly StringBuilder mWork = new StringBuilder();
        public readonly List<object> MenuItems = new List<object>();
        public int Page;
        string Title;
        readonly Menu Previous;
        readonly MenuModule Main;
        public Menu(MenuModule aMain, Menu aMenu, ModuleBase aModule) {
            Title = $"{aModule} Menu";
            Main = aMain;
            Previous = aMenu;
            foreach (var mm in aModule.MenuMethods) {
                MenuItems.Add(mm);
            }
        }
        public Menu(MenuModule aMain, Menu aMenu, List<IAccept> aList) {
            Title = "Main Menu";
            Main = aMain;
            Previous = aMenu;
            foreach (var acceptor in aList) {
                if (!(acceptor is MenuModule)) {
                    var mb = acceptor as ModuleBase;
                    if (mb.MenuName != null) {
                        MenuItems.Add(mb);
                    }
                }
            }
        }

        public void Input(string argument) {
            if (argument.Length == 2) {
                // menu number selection from !0 to !9
                int selection = 48 - argument[1];
                ModuleManager.logger.persist($"Menu input: '{selection}'");
                if (selection == 7) {
                    Main.SetMenu(Previous);
                } else if (selection == 8) {
                    // previous page
                    if (Page > 0) {
                        Page--;
                        Main.UpdateRequired = true;
                    }
                } else if (selection == 9) {
                    Page++;
                    Main.UpdateRequired = true;
                } else {
                    int index = selection * Page;
                    if (MenuItems.Count > index) {
                        HandleInput(MenuItems[index]);
                    }
                }
            }
        }
        void HandleInput(object aMenuItem) {
            ModuleBase mb = aMenuItem as ModuleBase;
            
            if (mb != null && mb.MenuMethods != null && mb.MenuMethods.Count > 0) {
                Main.SetMenu(new Menu(Main, this, mb));
            } else {
                var type = aMenuItem.GetType().Name;

                switch (type) { 
                    default:
                        Main.logger.persist($"Menu can't handle '{type}'");
                        break;
                }
            }
        }

        public string Update() {
            var index = Page * 7;
            if (MenuItems.Count < index) {
                Page = 0;
                index = 0;
            }
            Main.logger.log($"mMenuItems.Count={MenuItems.Count}");
            mWork.AppendLine(Title);
            for (int i = index; i < MenuItems.Count; i++) {
                mWork.Append(index + i + 1);
                mWork.Append(' ');
                var mi = MenuItems[i];
                var mm = mi as MenuMethod;
                if (mm != null) {
                    mWork.AppendLine(mm.Name);
                } else {
                    mWork.AppendLine(mi.ToString());
                }
                
            }
            var result = mWork.ToString();
            mWork.Clear();
            return result;
        }
    }
}