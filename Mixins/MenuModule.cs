using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript {
    //public delegate List<MenuItem> PaginationHandler(int page);
    public class MenuModule : Module<IMyTextPanel> {
        Menu CurrentMenu;
        
        public bool UpdateRequired = true;

        public MenuModule(ModuleManager aManager) : base(aManager) {
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
        
        public void SetMenu(Menu aMenu, bool aAssignPrevious = true) {
            if (aAssignPrevious) {
                aMenu.Previous = CurrentMenu;
            }
            CurrentMenu = aMenu;
            UpdateRequired = true;
        }
        public void Input(string argument) {
            CurrentMenu.Input(argument);
            UpdateRequired = true;
        }

        void UpdateAction() {
            if (UpdateRequired) {
                if (CurrentMenu == null) {
                    CurrentMenu = mManager.MainMenu(this);
                }
                UpdateRequired = false;
                var str = CurrentMenu.Update();
                if (str != null) {
                    foreach (var tp in Blocks) {
                        tp.WriteText(str);
                    }
                }
            }
        }
    }
}
