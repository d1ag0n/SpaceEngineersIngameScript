using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript {
    public partial class Program : MyGridProgram {
        readonly ModuleManager mManager;
        public Program() {
            try {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                mManager = new ModuleManager(this);
                var gy = new GyroModule(mManager);
                var th = new ThrustModule(mManager);
                mManager.Initialize();
                th.Active = gy.Active = false;
                var m = new CruiseMission(mManager);
                mManager.controller.NewMission(m);
                m.Input("off");
            } catch (Exception ex) {
                Me.CustomData = ex.ToString();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }

        public void Main(string argument, UpdateType updateSource) {
            try {
                
                mManager.Update(argument, updateSource);
            } catch (Exception ex) {
                Me.CustomData = ex.ToString();
                Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }
    }
}
