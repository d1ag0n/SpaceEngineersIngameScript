using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    public class MenuModule : Module<IMyTextPanel> {
        Menu CurrentMenu;
        public bool UpdateRequired = true;

        public MenuModule() {
            onUpdate = UpdateAction;
        }
        public override bool Accept(IMyCubeBlock aBlock) {
            var result = false;
            if (aBlock is IMyTerminalBlock) {
                var tb = aBlock as IMyTerminalBlock;
                if (tb.CustomData.Contains("#menuconsole")) {
                    result = base.Accept(aBlock);
                    if (result) {
                        var tp = aBlock as IMyTextPanel;
                        tp.CustomName = "Menu Console - " + Blocks.Count;
                        tp.ContentType = ContentType.TEXT_AND_IMAGE;
                        tp.Font = "Monospace";
                        if (tp.FontColor == Color.White) {
                            tp.FontColor = new Color(51, 255, 0);
                        }
                        Active = true;
                    }
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
                    CurrentMenu = ModuleManager.MainMenu(this);
                }
                var str = CurrentMenu.Update();
                foreach (var tp in Blocks) {
                    tp.WriteText(str);
                }
                UpdateRequired = false;
            }
        }
    }
}
