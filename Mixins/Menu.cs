using System.Text;
using System.Collections.Generic;

namespace IngameScript {
    public class Menu {
        readonly StringBuilder mWork = new StringBuilder();
        public readonly List<object> MenuItems = new List<object>();
        public int Page;
        string Title;
        public Menu Previous;
        readonly MenuModule Main;
        public Menu(MenuModule aMain, ModuleBase aModule) {
            Title = $"{aModule} Menu";
            Main = aMain;
            MenuItems = aModule.MenuMethods(0);
            
        }
        public Menu(MenuModule aMain, List<IAccept> aList) {
            Title = "Main Menu";
            Main = aMain;
            
            foreach (var acceptor in aList) {
                if (!(acceptor is MenuModule)) {
                    var mb = acceptor as ModuleBase;
                    if (mb.MenuName != null) {
                        MenuItems.Add(mb);
                    }
                }
            }
        }

        public Menu(MenuModule aMain, string aTitle, List<object> aMenuItems) {
            Main = aMain;
            Title = aTitle;
            MenuItems = aMenuItems;
        }


        public void Input(string argument) {
            if (argument.Length == 2) {
                // menu number selection from !0 to !9
                int selection = argument[1] - 48;
                ModuleManager.logger.persist($"Menu input: '{selection}'");
                if (selection == 6) {
                    Main.SetMenu(Previous, false);
                } else if (selection == 7) {
                    // previous page
                    if (Page > 0) {
                        Page--;
                        Main.UpdateRequired = true;
                    }
                } else if (selection == 8) {
                    Page++;
                    Main.UpdateRequired = true;
                } else {
                    int index = selection + (Page * 6);
                    ModuleManager.logger.persist($"Menu index: '{index}'");
                    if (MenuItems.Count > index) {
                        HandleInput(MenuItems[index]);
                    }
                }
            }
        }
        void HandleInput(object aMenuItem) {
            Main.logger.persist($"Menu.HandleInput({aMenuItem});");
            if (aMenuItem is ModuleBase) {
                Main.SetMenu(new Menu(Main, aMenuItem as ModuleBase));
            } else {
                var type = aMenuItem.GetType().Name;

                switch (type) {
                    case "MenuMethod":
                        var mm = aMenuItem as MenuMethod;
                        if (mm.Method != null) {
                            var menu = mm.Method(Main, mm.State);
                            if (menu != null) {
                                Main.SetMenu(menu);
                            }
                        }
                        break;
                    default:
                        Main.logger.persist($"Menu can't handle '{type}'");
                        break;
                }
                Main.UpdateRequired = true;
            }
        }

        public string Update() {
            var index = Page * 6;
            if (MenuItems.Count < index) {
                Page = 0;
                index = 0;
            }
            Main.logger.log($"mMenuItems.Count={MenuItems.Count}");
            mWork.AppendLine(Title);
            int count = 0;
            for (int i = index; i < MenuItems.Count && count < 7; i++) {
                count++;
                mWork.Append(index + i + 1);
                mWork.Append(' ');
                var mi = MenuItems[i];
                
                if (mi is MenuMethod) {
                    var mm = mi as MenuMethod;
                    mWork.AppendLine(mm.Name);
                } else if (mi is ModuleBase) {
                    var mb = mi as ModuleBase;
                    mWork.AppendLine(mb.MenuName);
                } else {
                    mWork.AppendLine(mi.ToString());
                }
            }
            while (count < 7) {
                count++;
                mWork.AppendLine();
            }
            mWork.AppendLine("7 Back");
            mWork.AppendLine("8 < Page");
            mWork.AppendLine("9 Page >");
            var result = mWork.ToString();
            mWork.Clear();
            return result;
        }
    }
}