using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    public abstract class MissionBase {

        
        
        


        protected BoundingSphereD mDestination;
        //protected readonly ThyDetectedEntityInfo mEntity;

        protected double PADDING = 10.0;
        protected double MAXVELO = 80.0;
        protected double mPreferredVelocityFactor = 1;
        protected double mMaxAccelLength;
        
        protected Vector3D Target;
        protected Vector3D mDirToDest;
        protected double mDistToDest;

        //protected BoundingSphereD Volume => mEntity == null ? mDestination : mEntity.WorldVolume;
        public bool Complete { get; protected set; }

        protected readonly ModuleManager mManager;
        protected ShipControllerModule mController => mManager.mController;
        public MissionBase(ModuleManager aManager) {
            mManager = aManager;
        }
        public abstract void Update();
        public virtual void Cancel() { }
        public virtual void Input(string arg) { }
    }
}
