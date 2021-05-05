using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public abstract class MissionBase {

        protected BoundingSphereD mDestination;
        protected double PADDING = 10.0;
        protected double MAXVELO = 99.99;
        protected double mPreferredVelocityFactor = 0.1;
        protected double mMaxAccelLength;
        
        protected Vector3D Target;
        protected Vector3D mDirToDest;
        protected double mDistToDest;

        //protected BoundingSphereD Volume => mEntity == null ? mDestination : mEntity.WorldVolume;
        public bool Complete { get; protected set; }

        protected readonly ModuleManager mManager;
        protected readonly LogModule mLog;
        protected readonly ShipControllerModule mController;
        public MissionBase(ModuleManager aManager) {
            mManager = aManager;
            mLog = mManager.mLog;
            aManager.GetModule(out mController);
        }
        public abstract void Update();
        public virtual bool Cancel() => true;
        public virtual void Input(string arg) { }

    }
}
