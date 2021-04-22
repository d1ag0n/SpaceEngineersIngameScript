using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript {
    public class GravDriveModule : Module<IMyGravityGenerator> {

        readonly Dictionary<long, GravDrive> mAM2GD = new Dictionary<long, GravDrive>();
        readonly List<GravDrive> mDrives = new List<GravDrive>();
        readonly ShipControllerModule mController;
        public GravDriveModule(ModuleManager aManager) : base(aManager) {
            onUpdate = init;
            aManager.GetModule(out mController);
        }

        public override bool Accept(IMyTerminalBlock aBlock) {
            var gg = aBlock as IMyGravityGenerator;
            if (gg == null) {
                return false;
            }
            identifyDrive(gg);
            return true;
        }

        void identifyDrive(IMyGravityGenerator gg) {
            var f = Base6Directions.GetIntVector(gg.Orientation.Forward);
            var r = Base6Directions.GetIntVector(gg.Orientation.Left);
            var d = -Base6Directions.GetIntVector(gg.Orientation.Up);
            var pos = gg.Position + d;

            var am = identifyAM(null, pos);
            if (am == null) {
                return;
            }
            GravDrive drive;
            if (mAM2GD.TryGetValue(am.EntityId, out drive)) {
                drive.AddGenerator(gg);
                return;
            }
            
            
            var start = Vector3I.Zero;
            while (true) {
                // move all the way to the "right"
                identifyMove(ref pos, r);
                // move all the way to the "front"
                identifyMove(ref pos, f);

                // cursor is now at "front right"
                if (start == Vector3I.Zero) {
                    start = pos;
                }
                // move all the way to the "left"
                identifyMove(ref pos, -r);

                // move all the way to the "back"
                identifyMove(ref pos, -f);

                am = identifyAM(null, pos + d);
                if (am == null) {
                    break;
                }
                pos += d;

            }
            
            var bb = BoundingBoxI.CreateFromPoints(new Vector3I[2] { start, pos });
            var ri = new Vector3I_RangeIterator(ref bb.Min, ref bb.Max);
            drive = new GravDrive(this, bb.Size.X + 1);
            mDrives.Add(drive);
            drive.index = mDrives.Count;
            drive.AddGenerator(gg);
            while (ri.IsValid()) {
                mAM2GD.Add(identifyAM(drive, ri.Current).EntityId, drive);
                ri.MoveNext();
            }

            
            
        }
        IMyArtificialMassBlock identifyAM(GravDrive drive, Vector3I pos) {
            var sb = Grid.GetCubeBlock(pos);
            if (sb == null) {
                return null;
            }
            var am = sb.FatBlock as IMyArtificialMassBlock;
            if (am == null) {
                return null;
            }
            drive?.AddMass(am);
            return am;
        }
        void identifyMove(ref Vector3I pos, Vector3I dir) {
            while (true) {
                var am = identifyAM(null, pos + dir);
                if (am == null) {
                    break;
                }
                pos += dir;
            }
        }
        void init() {
            foreach (var d in mDrives) {
                d.init();
            }
            onUpdate = update;
        }
        //Vector3D dir = MAF.ranDir();
        Vector3D dir = Vector3D.Forward;
        void update() {
            if (mController == null) {
                mLog.log("no controller");
                return;
            }
            if (mController.Remote == null) {
                mLog.log("no remote");
                return;
            }
            
            var m = mController.Remote.CalculateShipMass().PhysicalMass;
            var com = MAF.world2pos(mController.Remote.CenterOfMass, Grid.WorldMatrix);

            GravDrive weakest, d0, d1;
            weakest = d0 = d1 = null;
            //Vector3D accel;
            var t = double.MaxValue;
            //var dir = Vector3D.Backward;

            for (int i = 0; i < mDrives.Count; i++) {
                var d = mDrives[i];
                d.prep(dir, com, m);
                if (d.mTorque < t) {
                    t = d.mTorque;
                    if (weakest != null) {
                        if (d0 == null) {
                            d0 = weakest;
                        } else {
                            d1 = weakest;
                        }
                    }
                    weakest = d;
                } else {
                    if (d0 == null) {
                        d0 = d;
                    } else {
                        d1 = d;
                    }
                }
            }

            mLog.log(mLog.gps("weakest", MAF.local2pos(weakest.mACoM, Grid.WorldMatrix)));
            mLog.log(mLog.gps("d0", MAF.local2pos(d0.mACoM, Grid.WorldMatrix)));
            mLog.log(mLog.gps("d1", MAF.local2pos(d1.mACoM, Grid.WorldMatrix)));

            var weakest2com = com - weakest.mACoM;
            var weakest2d0 = d0.mACoM - weakest.mACoM;
            var d02d1 = d1.mACoM - d0.mACoM;

            var C = MAF.angleBetween(weakest2com, weakest2d0);
            mLog.log($"C={C:f3}");
            var A = MAF.angleBetween(-weakest2d0, d02d1);
            mLog.log($"A={A:f3}");
            var b = weakest2d0.Length();
            mLog.log($"b={b:f3}");
            var B = MathHelperD.Pi - A - C;
            mLog.log($"B={B:f3}");
            var c = (b * Math.Sin(C)) / Math.Sin(B);
            mLog.log($"c={c:f3}");
            // virtual artificial center of mass 
            //var vACoM = d1.mACoM + (d0.mACoM - d1.mACoM) / 2d;
            d02d1.Normalize();
            // virtual artificial center of mass modified
            
            var vACoM = d0.mACoM + d02d1 * c;
            mLog.log(mLog.gps("vACoM", MAF.local2pos(vACoM, Grid.WorldMatrix)));
            var pvACoM = MAF.orthoProject(vACoM, com, dir);
            var vMoment = Vector3D.Distance(pvACoM, com);
            var vAccel = 2d * (d0.mMaxAccelLen < d1.mMaxAccelLen ? d0.mMaxAccelLen : d1.mMaxAccelLen);
            var vTorque = vMoment * vAccel;

            if (vTorque > weakest.mTorque) {
                mLog.log("expected");
                
                var accel = weakest.mTorque / vMoment;

                var d0accel = accel / c;
                var d1accel = accel - d0accel;
                weakest.accel(dir, m);
                d0.accel(dir * d0accel, m);
                d1.accel(dir * d1accel, m);
            } else {
                mLog.log("unexpected");
            }
            

        }
    }
}
