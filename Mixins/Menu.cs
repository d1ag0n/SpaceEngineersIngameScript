using System.Text;
using System.Collections.Generic;
using System;
using VRageMath;

namespace IngameScript {
    public abstract class Menu {
        static int PageCount(int itemCount) => itemCount == 0 ? itemCount : (itemCount / 6) + 1;

        protected readonly List<MenuItem> mItems = new List<MenuItem>();
        readonly StringBuilder mWork = new StringBuilder();

        string mText;

        public string Title;

        public readonly MenuModule mMenuModule;
        protected ModuleManager mManager => mMenuModule.mManager;
        protected LogModule mLog => mManager.mLog;
        protected ShipControllerModule mController => mManager.mController;

        public int mPage;

        public abstract List<MenuItem> GetPage();

        public Menu(MenuModule aMenuModule) {
            mMenuModule = aMenuModule;
        }

        protected int PageIndex(int aPage, int aCount) => aCount > 0 ? (aPage % PageCount(aCount)) * 6 : 0;


        /*public void Input(int arg) {
            if (argument.Length == 1) {
                int selection;
                if (int.TryParse(argument, out selection)) {
                    selection = MathHelper.Clamp(selection, 0, 8);
                    mMenuModule.mLog.persist($"selection={selection}");
                    if (selection == 6) {
                        mMenuModule.SetMenu(Previous);
                    } else if (selection == 7) {
                        Page--;
                        mMenuModule.UpdateRequired = true;
                    } else if (selection == 8) {
                        Page++;
                        mMenuModule.UpdateRequired = true;
                    } else {
                        var items = Items();
                        if (items != null) {
                            var count = items.Count;
                            mMenuModule.mLog.persist($"count={count}");
                            if (count > 0) {
                                var pageIndex = PageIndex(Page, count);
                                var index = pageIndex + selection;
                                mMenuModule.mLog.persist($"pageIndex={pageIndex}");
                                mMenuModule.mLog.persist($"index={index}");
                                if (index < count) {
                                    HandleInput(items[index]);
                                }
                            }
                        }
                    }
                }
            } else if (argument.Length > 1) {
                // use inputmodules

                mMenuModule.mManager.UserInput = argument;
                mMenuModule.UpdateRequired = true;
            }
        }*/

        /*void HandleInput(MenuItem aMenuItem) {
            //Main.logger.persist($"Menu.HandleInput({aMenuItem});");
            var menu = aMenuItem.Run();

            if (menu != null) {
                mMenuModule.SetMenu(menu);
            } else {
                mMenuModule.UpdateRequired = true;
            }

        }*/

    }
}
