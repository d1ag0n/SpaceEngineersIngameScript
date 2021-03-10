using System.Text;
using System.Collections.Generic;
using System;

namespace IngameScript {
    public class Menu {
        readonly StringBuilder mWork = new StringBuilder();
        readonly Func<int, List<object>> MenuItems;
        public int Page;
        string Title;
        public Menu Previous;
        readonly MenuModule Main;
        List<object> Items;
        public Menu(MenuModule aMain, ModuleBase aModule) {
            Title = $"{aModule.MenuName}";
            Main = aMain;
            MenuItems = aModule.MenuMethods;
            Items = MenuItems(0);
        }
        public Menu(MenuModule aMain, List<IAccept> aList) {
            Title = "Main Menu";
            Main = aMain;

            MenuItems = p => {
                int index = p * 6;
                int count = 0;
                List<object> result = new List<object>();
                for (int i = index; i < aList.Count; i++) {
                    if (count == 6) {
                        break;
                    }
                    if (aList.Count > i) {
                        var acceptor = aList[i];
                        if (!(acceptor is MenuModule)) {
                            count++;
                            var mb = acceptor as ModuleBase;
                            if (mb.MenuName != null) {
                                result.Add(mb);
                            }
                        }
                    }
                }
                return result;
            };
        }

        public Menu(MenuModule aMain, string aTitle, Func<int, List<object>> aPaginator) {
            Main = aMain;
            Title = aTitle;
            MenuItems = aPaginator;
            Items = MenuItems(0);
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
                        Items = MenuItems(Page);
                        Main.UpdateRequired = true;
                    }
                } else if (selection == 8) {

                    var items = MenuItems(Page + 1);
                    if (items.Count > 0) {
                        Page++;
                        Main.UpdateRequired = true;
                        Items = items;
                    }
                    
                } else {
                    int index = selection + (Page * 6);
                    ModuleManager.logger.persist($"Menu index: '{index}'");
                    if (Items.Count > index) {
                        HandleInput(Items[index]);
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
            
            var items = MenuItems(Page);
            mWork.AppendLine(Title);
            int count = 0;
            foreach (var item in Items) { 
                count++;
                mWork.Append(count + 1);
                mWork.Append(' ');
                
                
                if (item is MenuMethod) {
                    var mm = item as MenuMethod;
                    mWork.AppendLine(mm.Name);
                } else if (item is ModuleBase) {
                    var mb = item as ModuleBase;
                    mWork.AppendLine(mb.MenuName);
                } else {
                    mWork.AppendLine(item.ToString());
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