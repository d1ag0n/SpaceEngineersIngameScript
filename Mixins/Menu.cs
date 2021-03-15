using System.Text;
using System.Collections.Generic;
using System;

namespace IngameScript {
    public class Menu {
        
        public delegate List<MenuItem> Paginator(int page);
        readonly StringBuilder mWork = new StringBuilder();
        readonly Paginator MenuItems;
        public int Page;
        string Title;
        public Menu Previous;
        readonly MenuModule Main;
        List<MenuItem> Items;
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        public Menu(MenuModule aMain, ModuleBase aModule) {
            Title = $"{aModule.MenuName}";
            Main = aMain;
            MenuItems = aModule.Menu;
            //Items = MenuItems(0);
        }
        public Menu(MenuModule aMain, List<ModuleBase> aList) {
            Title = "Main Menu";
            Main = aMain;
            
            MenuItems = p => {
                int index = p * 6;
                int count = 0;

                mMenuItems.Clear();
                for (int i = index; i < aList.Count; i++) {
                    if (count == 6) {
                        break;
                    }
                    var acceptor = aList[i];
                    
                    if (!(acceptor is MenuModule)) {
                        
                        
                        if (acceptor.MenuName != null) {
                            mMenuItems.Add(new MenuItem(acceptor));
                            count++;
                        }
                    }
                }
                return mMenuItems;
            };
            //Items = MenuItems(0);
        }

        public Menu(MenuModule aMain, string aTitle, Paginator aPaginator) {
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
                    Page--;
                    Main.UpdateRequired = true;
                } else if (selection == 8) {
                    Page++;
                    Main.UpdateRequired = true;
                } else {
                    if (Items.Count > selection) {
                        HandleInput(Items[selection]);
                    }
                }
            } else if (argument.Length > 1) {
                ModuleManager.UserInput = argument;
                Main.UpdateRequired = true;
            }
        }
        
        void HandleInput(MenuItem aMenuItem) {
            //Main.logger.persist($"Menu.HandleInput({aMenuItem});");
            if (aMenuItem.State is ModuleBase) {
                Main.SetMenu(new Menu(Main, aMenuItem.State as ModuleBase));
            } else {
                var menu = aMenuItem.Method?.Invoke(Main, aMenuItem.State);
                if (menu != null) {
                    Main.SetMenu(menu);
                } else {
                    Main.UpdateRequired = true;
                }
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

                if (item.Name == null) {
                    if (item.State is ModuleBase) {
                        var mb = (ModuleBase)item.State;
                        mWork.AppendLine(mb.MenuName);
                    } else {
                        mWork.AppendLine("Unknown Item in menu");
                    }
                } else {
                    mWork.AppendLine(item.Name);
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