using System.Text;
using System.Collections.Generic;
using System;

namespace IngameScript {
    public class Menu {
        
        
        readonly StringBuilder mWork = new StringBuilder();
        readonly PaginationHandler onPage;
        public int Page;
        string Title;
        public Menu Previous;
        readonly MenuModule mMain;
        List<MenuItem> Items;
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();
        public Menu(MenuModule aMain, ModuleBase aModule) {
            Title = $"{aModule.MenuName}";
            mMain = aMain;
            onPage = aModule.onPage;
            //Items = MenuItems(0);
        }
        public static int PageCount(int itemCount) => (itemCount / 6) + 1;
        public static int PageNumber(int pageNumber, int itemCount) => Math.Abs(pageNumber % PageCount(itemCount));
        public Menu(MenuModule aMain, List<ModuleBase> aList) {
            Title = "Main Menu";
            mMain = aMain;
            
            onPage = aPage => {

                int items = 0;
                foreach(var m in aList) {
                    if (m is MenuModule) continue;
                    if (m.MenuName == null) continue;
                    items++;
                }
                int index = PageNumber(aPage, items) * 6;
                int count = 0;

                mMenuItems.Clear();
                for (int i = index; i < aList.Count; i++) {
                    if (count == 6) {
                        break;
                    }
                    var m = aList[i];
                    
                    if (!(m is MenuModule)) {
                        if (m.MenuName != null) {
                            mMenuItems.Add(new MenuItem(m));
                            count++;
                        }
                    }
                }
                return mMenuItems;
            };
            //Items = MenuItems(0);
        }

        public Menu(MenuModule aMain, string aTitle, PaginationHandler aPaginator) {
            mMain = aMain;
            Title = aTitle;
            onPage = aPaginator;
            //Items = MenuItems(0);
        }


        public void Input(string argument) {
            if (argument.Length == 1) {
                // menu number selection from !0 to !9
                int selection = argument[0] - 48;
                //ModuleManager.logger.persist($"Menu input: '{selection}'");
                if (selection == 6) {
                    mMain.SetMenu(Previous, false);
                    mMain.UpdateRequired = true;
                } else if (selection == 7) {
                    Page--;
                    mMain.UpdateRequired = true;
                } else if (selection == 8) {
                    Page++;
                    mMain.UpdateRequired = true;
                } else {
                    if (Items.Count > selection) {
                        HandleInput(Items[selection]);
                    }
                }
            } else if (argument.Length > 1) {
                mMain.mManager.UserInput = argument;
                mMain.UpdateRequired = true;
            }
        }
        
        void HandleInput(MenuItem aMenuItem) {
            //Main.logger.persist($"Menu.HandleInput({aMenuItem});");
            if (aMenuItem.State is ModuleBase) {
                mMain.SetMenu(new Menu(mMain, aMenuItem.State as ModuleBase));
            } else {
                var menu = aMenuItem.Method?.Invoke(mMain, aMenuItem.State);
                if (menu != null) {
                    mMain.SetMenu(menu);
                } else {
                    mMain.UpdateRequired = true;
                }
            }
        }

        public string Update() {
            var index = Page * 6;

            mWork.AppendLine(Title);
            int count = 0;
            Items = onPage(Page);
            if (Items == null) {
                return null;
            }

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