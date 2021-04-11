using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript {
    class PeriscopeModule : Module<IMyMotorStator> {
        //readonly CameraModule mCamera;
        IMyMotorStator first, second;
        IMyCameraBlock _mCamera;
        public IMyCameraBlock mCamera => _mCamera;
        readonly List<MenuItem> mMenuMethods = new List<MenuItem>();
        public double Range = 20000;
        bool xneg = false;
        public PeriscopeModule(ModuleManager aManager) : base(aManager) {
            //aManager.GetModule(out mCamera);
            onUpdate = UpdateAction;
            //onSave = SaveDel;
            //onLoad = LoadDel;
        }
        void LoadDel(Serialize s, string aData) {
            var ar = aData.Split(Serialize.RECSEP);
            foreach (var record in ar) {
                var entry = record.Split(Serialize.UNTSEP);
                switch (entry[0]) {
                    case "range":
                        double.TryParse(entry[1], out Range);
                        break;
                }
            }
        }
        void SaveDel(Serialize s) {
            s.unt("range");
            s.str(Range);
        }

        public override bool Accept(IMyTerminalBlock b) {
            bool result = false;

            if (first == null) {

                if (b.CustomData.Contains("#periscope")) {
                    result = base.Accept(b);
                    if (result) {
                        first = b as IMyMotorStator;
                        first.ShowInTerminal = false;
                        if (first != null && first.TopGrid != null) {
                            mManager.GetByGrid(first.TopGrid.EntityId, ref second);
                            if (second != null && second.TopGrid != null) {
                                second.ShowInTerminal = false;
                                mManager.GetByGrid(second.TopGrid.EntityId, ref _mCamera);
                                if (mCamera != null) {
                                    mCamera.CustomName = $"!Periscope {first.CustomName} - Camera";
                                    mCamera.Enabled =
                                    mCamera.EnableRaycast = true;
                                    if (mCamera.Orientation.Left == second.Top.Orientation.Up) {
                                        xneg = true;
                                    }
                                    Okay = true;

                                    Active = true;
                                    Nactivate();
                                }
                            }
                        }
                    }
                }
                    
                
            } else {
                // multiple periscopes
                // new module need some way to make sure new module does not grab control of existing periscope
            }
            return result;
        }
        public void Nactivate() {
            if (Okay) {
                first.TargetVelocityRad = 0;
                second.TargetVelocityRad = 0;
                Active = !Active;
                if (Active) {
                    mCamera.CustomName = "!" + mCamera.CustomName;
                    mLog.persist($"View {mCamera.CustomName}");
                } else {
                    mCamera.CustomName = mCamera.CustomName.Substring(1);
                }
            }
        }
        

        
        void UpdateAction() {
            if (Active) {
                var sc = mController.Cockpit;
                mLog.log($"Periscope Controller={sc.CustomName}");
                if (sc != null) {
                    var rot = sc.RotationIndicator;
                    //logger.log(rot);
                    if (first == null) {
                        mLog.log("first null");
                    } else {
                        /* If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that might be below the plane, 
                         * compute the dot product v * (p−p0). This projects the vector to p on the up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively. */
                        var rad = rot.Y * 0.01f;
                        if (first.WorldMatrix.Up.Dot(mCamera.WorldMatrix.Up) < 0) {
                            rad = -rad;
                        }
                        first.TargetVelocityRad = rad;
                    }
                    if (second == null) {
                        mLog.log("second null");
                    } else {
                        var rad = rot.X * 0.01f;
                        if (xneg) {
                            rad = -rad;
                        }
                        second.TargetVelocityRad = rad;
                    }
                }
            }
        }
    }
}
