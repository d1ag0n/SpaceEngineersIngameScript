using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    partial class Program : MyGridProgram {

        delegate void main(string a, UpdateType u);
        
        main Update;

        readonly MenuModule mMenu;
        
        public Program() {
            Me.CustomName = "!Smart Pilot";
            ModuleManager.Initialize(this);
            new GyroModule();
            new ThrustModule();
            new CameraModule();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            if (Me.CustomData.Contains("mother")) {
                ModuleManager.logger.persist("I'm a Mother Ship");
                ModuleManager.Mother = true;
                new PeriscopeModule();
                mMenu = new MenuModule();
                new MotherShipModule();
                new ATCModule();
            } else if (Me.CustomData.Contains("#probe")) {
                ModuleManager.logger.persist("I'm a Probe");
                ModuleManager.Probe = true;
                new ProbeModule();
            } else if (Me.CustomData.Contains("#drill")) {
                ModuleManager.logger.persist("I'm a Drill");
                ModuleManager.Drill = true;
                new ATCLientModule();
            } else {
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
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
                    if (arg.Length > 0) {
                        if (arg == "save") {
                            Save();
                        } else if (mMenu != null) {
                            mMenu.Input(arg);
                        }
                    }
                }
                if ((type & UpdateType.IGC) != 0) {
                    
                }
                if ((type & UpdateType.Update10) != 0) {
                    ModuleManager.logger.log("PB Id ", Me.EntityId);
                    ModuleManager.Update();
                    if (ModuleManager.Probe) {
                        Echo(ModuleManager.logger.LastText);
                    }
                }
            } catch (Exception ex) {
                ModuleManager.logger.persist(ex.ToString());
                Echo(ex.ToString());
            }
        }
        public void Save() => Storage = ModuleManager.Save();
        public void Main(string a, UpdateType u) => Update(a, u);
    }
}
