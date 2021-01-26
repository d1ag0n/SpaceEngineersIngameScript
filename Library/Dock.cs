using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    class Dock
    {
        readonly GTS gts;
        readonly Logger g;
        public readonly IMyPistonBase X;
        public string Name {
            get {
                return C == null ? "unknown" : C.CustomName;
            }
        }
        public IMyPistonBase Y { get; private set; }
        public IMyPistonBase Z { get; private set; }
        IMyShipConnector C;
        States state = States.uninitialized;
        public Vector3D position { get; private set; }
        public Vector3D direction { get; private set; }
        Vector3D target;


        float xu = float.NaN;
        float xd = float.NaN;
        float xl = float.NaN;
        float xr = float.NaN;

        float yu = float.NaN;
        float yd = float.NaN;
        float yl = float.NaN;
        float yr = float.NaN;

        public enum States
        {
            uninitialized,
            initialized,
            precalibrated,
            calibrated,
            retracting,
            retracted,
            aligning,
            aligned,
            connected
        }
        float angleBetween(Vector3D a, Vector3D b) {
            var result = Math.Acos(a.Dot(b));
            //log("angleBetween ", result);
            return (float) result;
        }
        public Dock(GTS aGTS, Logger aLogger, IMyPistonBase aPiston) {
            gts = aGTS;
            g = aLogger;
            X = aPiston;
            X.Velocity = 0;
            X.Enabled = true;
        }
        void checkAngles() {
            checkAnglesX();
            checkAnglesY();
        }
        void checkAnglesX() {
            var mx = X.WorldMatrix;
            var cm = C.WorldMatrix;

            if (float.IsNaN(xu)) {
                var vxu = angleBetween(mx.Up, cm.Up);
                if (!float.IsNaN(vxu)) {
                    xu = vxu;
                }
            }

            if (float.IsNaN(xd)) {
                var vxd = (float)angleBetween(mx.Up, cm.Down);
                if (!float.IsNaN(vxd)) {
                    xd = vxd;
                }
            }

            if (float.IsNaN(xl)) {
                var vxl = (float)angleBetween(mx.Up, cm.Left);
                if (!float.IsNaN(vxl)) {
                    xl = vxl;
                }
            }

            if (float.IsNaN(xr)) {
                var vxr = (float)angleBetween(mx.Up, cm.Right);
                if (!float.IsNaN(vxr)) {
                    xr = vxr;
                }
            }
        }
        void checkAnglesY() {
            var my = Y.WorldMatrix;
            var cm = C.WorldMatrix;

            if (float.IsNaN(yu)) {
                var vyu = angleBetween(my.Up, cm.Up);
                if (!float.IsNaN(vyu)) {
                    yu = vyu;
                }
            }

            if (float.IsNaN(yd)) {
                var vyd = (float)angleBetween(my.Up, cm.Down);
                if (!float.IsNaN(vyd)) {
                    yd = vyd;
                }
            }

            if (float.IsNaN(yl)) {
                var vyl = (float)angleBetween(my.Up, cm.Left);
                if (!float.IsNaN(vyl)) {
                    yl = vyl;
                }
            }

            if (float.IsNaN(yr)) {
                var vyr = (float)angleBetween(my.Up, cm.Right);
                if (!float.IsNaN(vyr)) {
                    yr = vyr;
                }
            }
        }
        bool precalibrate() {
            X.Velocity =
            Y.Velocity =
            Z.Velocity = -0.5f;
            g.log("precalibrate");
            g.log("X ", X.CurrentPosition, " needs ", X.MinLimit);
            g.log("Y ", Y.CurrentPosition, " needs ", Y.MinLimit);
            g.log("Z ", Z.CurrentPosition, " needs ", Z.MinLimit);
            return X.CurrentPosition == X.MinLimit &&
                Y.CurrentPosition == Y.MinLimit &&
                Z.CurrentPosition == Z.MinLimit;
        }
        bool calibrate() {
            g.log("dock calibration ", state);
            if (state == States.precalibrated) {
                var cx = calibrate(X);
                var cy = calibrate(Y);
                var cz = calibrate(Z);
                if (cx && cy && cz) {
                    position = C.WorldMatrix.Translation;
                    direction = C.WorldMatrix.Forward;
                    return true;
                }
            }
            return false;
        }
        bool calibrate(IMyPistonBase aPiston) {
            var result = false;
            
            aPiston.MinLimit = 0.0f;
            aPiston.MaxLimit = 9.7f;

            var mid = (aPiston.MaxLimit - aPiston.MinLimit) * 0.5;

            var dif = mid - aPiston.CurrentPosition;
            if (Math.Abs(dif) < 0.1) {
                aPiston.Velocity = 0;
                result = true;
            } else {
                aPiston.Velocity = (float)dif;
            }
            g.log("pistion calibration dif ", dif);
            g.log("pistion calibration mid ", mid);
            return result;
        }
        public bool retract() {
            C.Enabled = false;
            X.Velocity = 
            Y.Velocity = 
            Z.Velocity = -1.0f;
            var result = X.CurrentPosition == X.MinLimit && Y.CurrentPosition == Y.MinLimit && Z.CurrentPosition == Z.MinLimit;
            if (result) {
                state = States.retracted;
            } else {
                state = States.retracting;
            }
            return result;
        }
        Vector3D world2pos(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world - local.Translation, MatrixD.Transpose(local));
        public void update() {
            g.log("dock state ", state);
            switch (state) {
                case States.aligning:
                    if (align(world2pos(target, C.WorldMatrix))) {
                        state = States.aligned;
                    }
                    break;
                case States.aligned:
                    align(world2pos(target, C.WorldMatrix));
                    extend();
                    break;
                case States.connected:
                    if (C.Status != MyShipConnectorStatus.Connected) {
                        retract();
                    }
                    break;
            }
        }
        public void setAlign(Vector3D aTarget) {
            target = aTarget;
            if (state != States.aligned) {
                state = States.aligning;
            }
        }
        bool align(Vector3D aTarget) {
            aTarget.Y *= 10.0;
            aTarget.X *= 10.0;
            g.log("aligning ", aTarget);
            if (xu < 1) {
                X.Velocity = (float)aTarget.Y;
            } else if (xd < 1) {
                X.Velocity = (float)-aTarget.Y;
            } else if (xl < 1) {
                X.Velocity = (float)-aTarget.X;
            } else {
                X.Velocity = (float)aTarget.X;
            }

            if (yu < 1) {
                Y.Velocity = (float)aTarget.Y;
            } else if (yd < 1) {
                Y.Velocity = (float)-aTarget.Y;
            } else if (yl < 1) {
                Y.Velocity = (float)-aTarget.X;
            } else {
                Y.Velocity = (float)aTarget.X;
            }
            
            var result = Y.Velocity < 0.1f && X.Velocity < 0.1f;
            if (result) {
                Y.Velocity = X.Velocity = 0;
            }
            return result;

        }
        public bool extend() {
            C.Enabled = true;
            var result = false;
            switch (C.Status) {
                case MyShipConnectorStatus.Connectable:
                    // 0.00015
                    // 0.00001
                    if (C.PullStrength == 1.0) {
                        C.Connect();
                    } else {
                        C.PullStrength *= 1.1f;
                    }
                    
                    g.log("Strength: ", C.PullStrength);
                    result = true;
                    break;
                case MyShipConnectorStatus.Unconnected:
                    C.PullStrength = 0.001f;
                    
                    Z.Velocity = 0.25f;
                    break;
                case MyShipConnectorStatus.Connected:
                    state = States.connected;
                    break;
            }
            return result;
        }
        public bool init() {
            g.log("Dock state ", state);
            if (state == States.uninitialized) {

                var plist = new List<IMyPistonBase>();
                var clist = new List<IMyShipConnector>();

                gts.initList(plist);
                
                var top = X.TopGrid;
                for (int i = 0; i < plist.Count; i++) {
                    var p = plist[i];
                    if (p.CubeGrid.EntityId == top.EntityId) {
                        Y = p;                        
                        Y.Velocity = 0;
                        Y.Enabled = true;
                        break;
                    }
                }

                if (Y != null) {
                    top = Y.TopGrid;
                    for (int i = 0; i < plist.Count; i++) {
                        var p = plist[i];
                        if (p.CubeGrid.EntityId == top.EntityId) {
                            Z = p;
                            Z.Velocity = 0;
                            Z.Enabled = true;
                            break;
                        }
                    }
                }

                if (Z != null) {
                    top = Z.TopGrid;
                    gts.initList(clist);
                    for (int i = 0; i < clist.Count; i++) {
                        var c = clist[i];
                        if (c.CubeGrid.EntityId == top.EntityId) {
                            C = c;
                            break;
                        }
                    }
                    if (C != null) {
                        state = States.initialized;
                    }
                }
            } else if (state == States.initialized) {
                if (precalibrate()) {
                    state = States.precalibrated;
                }
            } else if (state == States.precalibrated) {
                if (calibrate()) {
                    state = States.calibrated;
                    return true;
                }
            }
            checkAngles();
            return false;
        }
    }
}
