using System.Text;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using System;

namespace IngameScript {
    public class MenuModule : Module<IMyTextPanel> {

        
        
        
        Menu CurrentMenu;
        public bool UpdateRequired = true;
        public override bool Accept(IMyTerminalBlock aBlock) {
            var result = false;
            if (aBlock.CustomData.Contains("#menuconsole")) {
                result = base.Accept(aBlock);
                if (result) {
                    var tp = aBlock as IMyTextPanel;
                    tp.CustomName = "LCD - Menu Console #" + Blocks.Count;
                    tp.ContentType = ContentType.TEXT_AND_IMAGE;
                }
            }
            return result;
        }
        
        public void SetMenu(Menu aMenu) {
            CurrentMenu = aMenu;
            UpdateRequired = true;
        }
        public void Input(string argument) =>
            CurrentMenu.Input(argument);

        public override void Update() {
            if (UpdateRequired) {
                if (CurrentMenu == null) {
                    CurrentMenu = new Menu(this, null, ModuleManager.mModules);
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
