using Sandbox;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Plugins;
using VRage.Scripting;
using Sandbox.Game.Gui;
namespace PBExtra {
    public class Plugin : IPlugin {
        public void Init(object gameInstance) {
            // this doesn't work because key already exists
            //using(var whitelist = MyScriptCompiler.Static.Whitelist.OpenBatch())
            //{
            //    whitelist.AllowTypes(MyWhitelistTarget.Ingame, typeof(System.Diagnostics.Stopwatch));
            //}

            // therefore I just change the target on the existing key
            string whitelistKey = "System.Diagnostics.Stopwatch+*, System";

            var whitelistField = typeof(MyScriptWhitelist).GetField("m_whitelist", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (whitelistField == null)
                throw new Exception("whitelistField = null");

            var whitelist = (Dictionary<string, MyWhitelistTarget>)whitelistField.GetValue(MyScriptCompiler.Static.Whitelist);

            whitelist[whitelistKey] = MyWhitelistTarget.Both;
            MethodInfo method = typeof(MyGuiScreenEditor).GetMethod("TextTooLong", BindingFlags.Instance | BindingFlags.Public);
            MethodUtil.ReplaceMethod(typeof(Plugin).GetMethod("TextTooLong", BindingFlags.Instance | BindingFlags.Public), method);

        }
        public bool TextTooLong() => false;
        public void Dispose() {
        }

        public void Update() {
        }
    }
}
