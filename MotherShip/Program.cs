using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public partial class Program : MyGridProgram {
        readonly ModuleManager mManager;
        public Program() {
            mManager = new ModuleManager(this);
            if (!Me.CustomData.Contains("#mother")) {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            mManager.Mother = true;
            new GridComModule(mManager);
            new GyroModule(mManager);
            new ThrustModule(mManager);
            new CameraModule(mManager);
            new PeriscopeModule(mManager);
            new MotherShipModule(mManager);
            new ATCModule(mManager);
            new MenuModule(mManager, null);
            mManager.Initialize();
            mManager.mLog.persist("I'm a Mother Ship");
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        public void Save() { }
        public void Main(string arg, UpdateType aType) => mManager.Update(arg, aType);
    }
}
