using VRage;
using VRageMath;

namespace IngameScript {
    class MotherState {
        readonly ModuleManager mManager;
        readonly LogModule mLog;
        MatrixD _Matrix;
        public MatrixD Matrix {
            get {
                // Whiplash141 - https://discord.com/channels/125011928711036928/216219467959500800/825805636691951626
                // cross(angVel, displacement)
                // Where angVel is in rad / s and displacement is measured from the CoM pointing towards the point of interest
                var m = _Matrix;
                //logger.log(logger.gps("Position", _MotherMatrix.Translation));
                var d = mManager.Runtime - LastUpdate;
                //d += 0.0166;

                d += 1.0 / 60.0;
                if (!AngularVelo.IsZero()) {
                    var ng = AngularVelo * d;
                    var len = ng.Normalize();
                    var rot = MatrixD.CreateFromAxisAngle(ng, len);
                    var comDisp = m.Translation - CoM;
                    m *= rot;
                    m.Translation = CoM + Vector3D.Transform(comDisp, rot);
                }
                m.Translation += VeloDir * (Speed * d);
                return m;
            }
            private set {
                _Matrix = value;
            }
        }
        public long Id {
            get; private set;
        }
        public BoundingSphereD Sphere => BoundingSphereD.CreateFromBoundingBox(Box);
        public BoundingBoxD Box {
            get; private set;
        }
        public Vector3D CoM {
            get; private set;
        }
        public Vector3D VeloDir {
            get; private set;
        }
        public double Speed {
            get; private set;
        }
        public double LastUpdate {
            get; private set;
        }
        public Vector3D AngularVelo {
            get; private set;
        }
        public Vector3D VeloAt(Vector3D aPos) {
            if (AngularVelo.IsZero()) {
                return Vector3D.Zero;
            }
            return AngularVelo.Cross(MAF.local2pos(aPos, _Matrix) - CoM);
        }
        public MotherState(ModuleManager aManager) {
            mManager = aManager;
            aManager.GetModule(out mLog);
        }
        public void Update(Envelope aMessage) {
            Id = aMessage.Message.Source;
            mLog.log("MotherId ", Id);
            var ms = Unpack(aMessage.Message.Data);
            Box = ms.Item1;
            VeloDir = ms.Item2;
            Speed = ms.Item3;
            mLog.log("MotherSpeed ", Speed);
            AngularVelo = ms.Item4;
            Matrix = ms.Item5;
            CoM = ms.Item6;
            LastUpdate = aMessage.Time;
        }
        public static MyTuple<BoundingBoxD, Vector3D, double, Vector3D, MatrixD, Vector3D>
            Pack(BoundingBoxD volume, Vector3D dir, double speed, Vector3D ngVelo, MatrixD orientation, Vector3D aCoM) =>
            MyTuple.Create(volume, dir, speed, ngVelo, orientation, aCoM);

        public static MyTuple<BoundingBoxD, Vector3D, double, Vector3D, MatrixD, Vector3D>
            Unpack(object data) =>
            (MyTuple<BoundingBoxD, Vector3D, double, Vector3D, MatrixD, Vector3D>)data;
    }
}
