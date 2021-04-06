using Sandbox.ModAPI.Ingame;
using System;
using VRageMath;

namespace IngameScript {
    public partial class Program : MyGridProgram {
        
        delegate void main(string a, UpdateType u);
        bool reset = false;
        
        main Update;

        readonly MenuModule mMenu;

        readonly ModuleManager mManager;
        readonly GridCom mCom;
        
        public Program() {
            mCom = new GridCom(IGC);
            mManager = new ModuleManager(this);
            


            new GyroModule(mManager);
            new ThrustModule(mManager);
            new CameraModule(mManager);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            if (Me.CustomData.Contains("mother")) {
                mManager.logger.persist("I'm a Mother Ship");
                mManager.Mother = true;
                new PeriscopeModule(mManager);
                mMenu = new MenuModule(mManager);
                new MotherShipModule(mManager);
                new ATCModule(mManager);
            } else if (Me.CustomData.Contains("#probe")) {
                mManager.logger.persist("I'm a Probe");
                mManager.Probe = true;
                new ProbeModule(mManager);
            } else if (Me.CustomData.Contains("#drill")) {
                mManager.logger.persist("I'm a Drill");
                mManager.Drill = true;
                new ATCLientModule(mManager);
            } else {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            mManager.Initialize();
            Update = load;
        }
        public void Reset() {
            Storage = "";
            reset = true;
        }

        void load(string a, UpdateType u) {
            try {
                mManager.Load(Storage);
                Update = mManager.Update;
            } catch (Exception ex) {
                mManager.logger.persist(ex.ToString());
            }
        }

        public void Save() {
            if (reset) {
                reset = false;
            } else {
                Storage = mManager.Save();
            }
        }
        public void Main(string arg, UpdateType aType) {
            mManager.NotifyRun(Runtime);
            if ((aType & UpdateType.IGC) != 0) {
                mCom.MailCall(Runtime);
            }
            Update(a, u);
        }
    }
}
