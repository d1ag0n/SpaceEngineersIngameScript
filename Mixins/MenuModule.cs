using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using System.Collections.Generic;
using System.Text;
namespace IngameScript {
    //public delegate List<MenuItem> PaginationHandler(int page);
    public class MenuModule : Module<IMyTextPanel> {
        readonly Stack<Menu> mStack = new Stack<Menu>();
        readonly StringBuilder mWork = new StringBuilder();
        Menu mCurrent;
        List<MenuItem> mPage = new List<MenuItem>();

        bool mUpdateRequired = true;

        public MenuModule(ModuleManager aManager, Menu aMainMain) : base(aManager) {
            onUpdate = UpdateAction;
        }
        public override bool Accept(IMyTerminalBlock b) {
            var result = false;
            if (b.CustomData.Contains("#menuconsole")) {
                result = base.Accept(b);
                if (result) {
                    var tp = b as IMyTextPanel;
                    tp.CustomName = "Menu Console - " + Blocks.Count;
                    tp.ContentType = ContentType.TEXT_AND_IMAGE;
                    tp.Font = "DEBUG";
                    if (tp.FontColor == Color.White) {
                        tp.FontColor = new Color(51, 255, 0);
                    }
                    Active = true;
                }
            }
            return result;
        }
        void GoPrevious() {
            if (mStack.Count > 0) {
                mCurrent = mStack.Pop();
                mUpdateRequired = true;
            }
        }
        void SetMenu(Menu aMenu) {
            if (mCurrent != null) {
                mStack.Push(mCurrent);
            }
            mCurrent = aMenu;
        }
        public void Input(string argument) {
            int selection;
            if (int.TryParse(argument, out selection)) {
                selection = MathHelper.Clamp(selection, 0, 8);
                if (selection == 6) {
                    GoPrevious();
                } else if (selection == 7) {
                    mCurrent.mPage--;
                } else if (selection == 8) {
                    mCurrent.mPage++;
                } else {
                    var mi = mPage[selection];
                    var runResult = mi.Run();
                    if (runResult == null) {
                        GoPrevious();
                    } else if (runResult == mCurrent) {
                        
                    } else {
                        SetMenu(runResult);
                    }
                }
                mUpdateRequired = true;
            }
        }

        void UpdateAction() {
            if (mUpdateRequired && mCurrent != null) {
                mPage =  mCurrent.GetPage();
                var str = GetText();
                foreach (var tp in Blocks) {
                    tp.WriteText(str);
                }
            }
        }

        string GetText() {
            int count = 1;

            mWork.AppendLine(mCurrent.Title);

            if (mPage != null && mPage.Count > 0) {
                foreach (var mi in mPage) {
                    mWork.Append(count);
                    mWork.Append(' ');
                    mWork.Append(mi.mName);
                    if (count == 6) {
                        break;
                    }
                    count++;
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
