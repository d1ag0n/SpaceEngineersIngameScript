using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {

        delegate void main(string a, UpdateType u);

        main Update;

        readonly MenuModule mMenu;
        readonly Lag mLag = new Lag(90);
        
        public Program() {
            ModuleManager.Initialize(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            new GyroModule();
            new ThrustModule();
            new CameraModule();
            new PeriscopeModule();
            mMenu = new MenuModule();
            Update = load;
        }

        void load(string a, UpdateType u) {
            try {
                ModuleManager.Load(Storage);
            } catch (Exception ex) {
                ModuleManager.logger.persist(ex.ToString());
            }
            Update = loop;
        }
        void loop(string arg, UpdateType type) {
            try {
                if ((type & (UpdateType.Terminal | UpdateType.Trigger)) != 0) {
                    mMenu.Input(arg);
                }
                if ((type & UpdateType.IGC) != 0) {

                }
                if ((type & UpdateType.Update10) != 0) {
                    ModuleManager.logger.log("lag ", mLag.update(Runtime.LastRunTimeMs));
                    if (arg.Length > 0) {
                        if (arg == "save") {
                            Save();
                        } else {

                        }
                    }
                    ModuleManager.Update();
                }
            } catch (Exception ex) {
                ModuleManager.logger.persist(ex.ToString());
            }
        }
        public void Save() => Storage = ModuleManager.Save();
        public void Main(string a, UpdateType u) => Update(a, u);
    }
}
