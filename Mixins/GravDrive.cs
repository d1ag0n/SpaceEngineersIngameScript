using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;

namespace IngameScript {
    class GravDrive : BlockDirList<IMyGravityGenerator> {
        readonly GravDriveModule mModule;
        List<IMyArtificialMassBlock> mMasses = new List<IMyArtificialMassBlock>();
        List<IMyGravityGenerator> mGravs = new List<IMyGravityGenerator>();
        BoundingBoxD mBBMass;
        public Vector3D mACoM;
        readonly int mSize;
        double mMass;
        readonly ShipControllerModule mController;
        double[] forces = new double[6];
        public GravDrive(GravDriveModule aGrav, int aSize) {
            mModule = aGrav;
            mSize = aSize;
            aGrav.GetModule(out mController);
        }
        public void AddMass(IMyArtificialMassBlock am) {
            if (mMasses.Contains(am)) {
                throw new Exception("Artificial mass already a member of this drive.");
            }
            am.Enabled =
            am.ShowInTerminal = false;
            mMasses.Add(am);
            

            if (mMasses.Count == 1) {
                mBBMass = getBB(am);
            } else {
                mBBMass = mBBMass.Include(getBB(am));
            }
        }
        BoundingBoxD getBB(IMyCubeBlock b) => new BoundingBoxD(((Vector3D)b.Min - Vector3D.Half) * 2.5, ((Vector3D)b.Max + Vector3D.Half) * 2.5);
        public void AddGenerator(IMyGravityGenerator gg) {
            if (mGravs.Contains(gg)) {
                throw new Exception("Generator already a member of this drive.");
            }
            Add(gg, Base6Directions.Direction.Down);
            mGravs.Add(gg);
            gg.ShowOnHUD = false;
            gg.ShowInTerminal = false;
            gg.GravityAcceleration = 0f;
        }
        public void init() {
            mMass = 50000d * mSize;
            calculateACoM();

            var min = mSize * 2.5;
            var max = ((mSize * 2.5) - 1.25) * 2d;
            foreach (var gg in mGravs) {
                var bb = mBBMass.Include(getBB(gg));
                var fs = bb.Max - bb.Min;
                fs.X = MathHelper.Clamp(fs.X, min, max);
                fs.Y = (mSize * 2.5 + 1.25) * 2d;
                fs.Z = MathHelper.Clamp(fs.Z, min, max);
                gg.FieldSize = fs;
            }
            for (int i = 0; i < 6; i++) {
                forces[i] = mLists[i].Count * 9.81 * mMass;
            }
        }
        void calculateACoM() {
            var aCoM = Vector3D.Zero;
            double total = 0;
            
            foreach (var am in mMasses) {
                
                aCoM += am.Position * 2.5;
                total++;
            }
            mACoM = aCoM / total;
        }
        public string info() {
            var am = mMasses[0];
            return $"min={am.Min}, max={am.Max}";
            //$"genCount={mGravs.Count}, massCount={mMasses.Count}";
        }

        public void Update(Vector3D aForce) {

        }
        public Vector3D MaxAccel(Vector3D aLocalVec, double aMass) {
            //int f = 1, l = 3, u = 4;
            //if (aLocalVec.Z < 0) { f = 0; }
            //if (aLocalVec.X < 0) { l = 2; }
            //if (aLocalVec.Y < 0) { u = 5; }

            var amp = aLocalVec * 1000.0;
            var z = Math.Abs(amp.Z) * aMass;
            var x = Math.Abs(amp.X) * aMass;
            var y = Math.Abs(amp.Y) * aMass;


            double ratio = (forces[0] + forces[1]) / z;
            double tempRatio = (forces[2] + forces[3]) / x;

            if (tempRatio < ratio) {
                ratio = tempRatio;
            }
            tempRatio = (forces[4] + forces[5]) / y;
            if (tempRatio < ratio) {
                ratio = tempRatio;
            }
            var result = new Vector3D(x, y, z);
            if (ratio < 1.0) {
                result *= ratio;
            }
            result /= aMass;

            if (aLocalVec.Z < 0) {
                result.Z *= -1.0;
            }
            if (aLocalVec.X < 0) {
                result.X *= -1.0;
            }
            if (aLocalVec.Y < 0) {
                result.Y *= -1.0;
            }
            return result;
        }

        public Vector3D mMaxAccel;
        public double mMaxAccelLen;
        double mMomentArm;
        public double mTorque;
        public void prep(Vector3D dir, Vector3D com, double mass) {
            mMaxAccel = MaxAccel(dir, mass);
            var pacom = MAF.orthoProject(mACoM, com, dir);
            mMomentArm = Vector3D.Distance(pacom, com);
            mMaxAccelLen = mMaxAccel.Length();
            mTorque = mMaxAccelLen * mMomentArm;
        }
        public void accel(Vector3D accel) {

        }
    }
}