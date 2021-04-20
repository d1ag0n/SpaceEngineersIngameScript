using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;

namespace IngameScript {
    class GravDrive : BlockDirList<IMyGravityGenerator> {
        readonly GravDriveModule mGrav;
        List<IMyArtificialMassBlock> mMasses = new List<IMyArtificialMassBlock>();
        List<IMyGravityGenerator> mGravs = new List<IMyGravityGenerator>();
        public GravDrive(GravDriveModule aGrav) {
            mGrav = aGrav;
        }
        public void AddMass(IMyArtificialMassBlock am) {
            if (mMasses.Contains(am)) {
                throw new Exception("Artificial mass already a member of this drive.");
            }
            am.Enabled = false;
            mMasses.Add(am);
        }
        public void AddGenerator(IMyGravityGenerator gg) {
            if (mGravs.Contains(gg)) {
                throw new Exception("Generator already a member of this drive.");
            }
            Add(gg);
            mGravs.Add(gg);
            gg.GravityAcceleration = 0f;
        }
        public string info => $"genCount={mGravs.Count}, massCount={mMasses.Count}";
    }
}