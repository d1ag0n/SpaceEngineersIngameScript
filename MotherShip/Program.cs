using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public partial class Program : MyGridProgram {
        readonly ModuleManager mManager;
        readonly PersistenceModule mPersistence;
        public Program() {
            mManager = new ModuleManager(this);
            mManager.Mother = true;
            new GridComModule(mManager);
            new GyroModule(mManager);
            new ThrustModule(mManager);
            var cam = new CameraModule(mManager);
            new PeriscopeModule(mManager);
            new MotherShipModule(mManager);
            new ATCModule(mManager);
            var menu = new MenuModule(mManager);
            menu.SetMenu(new MotherShipMenu(menu));

            mPersistence = new PersistenceModule(mManager);
            mPersistence.Add(new CameraPersistence(cam));
            try {
                mPersistence.onLoad(Storage);
            }catch(Exception ex) {
                mManager.mLog.persist(ex.ToString());
            }

            mManager.Initialize();
            mManager.mLog.persist("I'm a Mother Ship");
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            
        }


        public void Save() {
            Storage = mPersistence.onSave();
        }
        public void Main(string arg, UpdateType aType) => mManager.Update(arg, aType);
    }
}
