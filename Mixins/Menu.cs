using System.Text;
using System.Collections.Generic;
using System;

namespace IngameScript {
    public class Menu {
        readonly StringBuilder mWork = new StringBuilder();
        readonly ModuleBase.delMenu MenuItems;
        public int Page;
        string Title;
        public Menu Previous;
        readonly MenuModule Main;
        List<object> Items;
        public Menu(MenuModule aMain, ModuleBase aModule) {
            Title = $"{aModule.MenuName}";
            Main = aMain;
            MenuItems = aModule.Menu;
            //Items = MenuItems(0);
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
                    var acceptor = aList[i];
                    
                    if (!(acceptor is MenuModule)) {
                        
                        var mb = acceptor as ModuleBase;
                        if (mb.MenuName != null) {
                            result.Add(mb);
                            count++;
                        }
                    }
                }
                return result;
            };
            //Items = MenuItems(0);
        }

        public Menu(MenuModule aMain, string aTitle, ModuleBase.delMenu aPaginator) {
            Main = aMain;
            Title = aTitle;
            MenuItems = aPaginator;
            //Items = MenuItems(0);
        }


        public void Input(string argument) {
            if (argument.Length == 1) {
                // menu number selection from !0 to !9
                int selection = argument[0] - 48;
                //ModuleManager.logger.persist($"Menu input: '{selection}'");
                if (selection == 6) {
                    Main.SetMenu(Previous, false);
                    Main.UpdateRequired = true;
                } else if (selection == 7) {
                    // previous page
                    if (Page > 0) {
                        Page--;
                        //Items = MenuItems(Page);
                        Main.UpdateRequired = true;
                    }
                } else if (selection == 8) {
                    var items = MenuItems(Page + 1);
                    if (items.Count > 0) {
                        Page++;
                        Main.UpdateRequired = true;
                        //Items = items;
                    }
                } else {
                    if (Items.Count > selection) {
                        HandleInput(Items[selection]);
                    }
                }
            }
        }
        void HandleInput(object aMenuItem) {
            //Main.logger.persist($"Menu.HandleInput({aMenuItem});");
            if (aMenuItem is ModuleBase) {
                Main.SetMenu(new Menu(Main, aMenuItem as ModuleBase));
            } else {
                var type = aMenuItem.GetType().Name;

                switch (type) {
                    case "MenuMethod":
                        var mm = (MenuMethod)aMenuItem;
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
            
            mWork.AppendLine(Title);
            int count = 0;
            Items = MenuItems(Page);
            foreach (var item in Items) {
                mWork.Append(++count);
                mWork.Append(' ');


                if (item is MenuMethod) {
                    var mm = (MenuMethod)item;
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
            mWork.AppendLine("7 Previous Menu");
            mWork.AppendLine("8 - 9 < Page >");
            var result = mWork.ToString();
            mWork.Clear();
            return result;
        }
    }
}