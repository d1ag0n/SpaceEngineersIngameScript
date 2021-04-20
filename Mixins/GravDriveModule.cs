using System;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript {
    public class GravDriveModule : Module<IMyGravityGenerator> {

        readonly Dictionary<long, GravDrive> mAM2GD = new Dictionary<long, GravDrive>();
        readonly List<GravDrive> mDrives = new List<GravDrive>();

        public GravDriveModule(ModuleManager aManager) : base(aManager) {
            onUpdate = update;
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
                    mLog.persist($"start={start}");
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
            var end = pos;

            mLog.persist($"end={end}");
            /*
            var top = start;
            var bottom = end;

            if (end.X > top.X) {
                top.X = end.X;
            }
            if (end.Y > top.Y) {
                top.Y = end.Y;
            }
            if (end.Z > top.Z) {
                top.Z = end.Z;
            }
            if (start.X < bottom.X) {
                bottom.X = start.X;
            }
            if (start.Y < bottom.Y) {
                bottom.Y = start.Y;
            }
            if (start.Z < bottom.Z) {
                bottom.Z = start.Z;
            }*/
            var points = new Vector3I[2] { start, end };
            var bb = BoundingBoxI.CreateFromPoints(points);
            var ri = new Vector3I_RangeIterator(ref bb.Min, ref bb.Max);
            


            drive = new GravDrive(this);
            drive.AddGenerator(gg);
            while (ri.IsValid()) {
                mAM2GD.Add(identifyAM(drive, ri.Current).EntityId, drive);
                ri.MoveNext();
            }

            
            mDrives.Add(drive);
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

        void update() {
            for (int i = 0; i < mDrives.Count; i++) {
                mLog.log($"{i + 1}: {mDrives[i].info}");
            }
        }
    }
}
