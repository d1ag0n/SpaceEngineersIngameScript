using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    // todo make a mission?
    public class PeriscopeMenu : Menu {
        readonly PeriscopeModule mPeriscope;
        readonly CameraModule mCamera;
        public PeriscopeMenu(MenuModule aMenuModule) : base(aMenuModule) {
            Title = "Periscope";
            mManager.GetModule(out mPeriscope);
            mManager.GetModule(out mCamera);
        }

        public override List<MenuItem> GetPage() {
            if (mPeriscope == null) {
                mLog.persist("mPeriscope=null");
            } else {
                if (mPeriscope.mCamera == null) {
                    mLog.persist("mPeriscope.mCamera=null");
                }
            }
            
            mItems.Clear();
            mItems.Add(new MenuItem(mPeriscope.Active ? "Deactivate" : "Activate", Nactivate));
            mItems.Add(new MenuItem($"Periscope Scan {mPeriscope.mCamera.AvailableScanRange:f0}m Available", scan));
            mItems.Add(new MenuItem("Fly Out", () => {
                var pos = mPeriscope.mCamera.WorldMatrix.Translation + mPeriscope.mCamera.WorldMatrix.Forward * mPeriscope.Range;
                mController.NewMission(new Mission(mManager, pos));
                return this;
            }));
            mItems.Add(new MenuItem("Camera Module Scan", moduleScan));
            //mMenuMethods.Add(new MenuItem("Planetary Scan", null, PlanetScan));
            mItems.Add(new MenuItem($"Increase Range {mPeriscope.Range:f0}", range(1.25)));
            mItems.Add(new MenuItem($"Decrease Range {mPeriscope.Range:f0}", range(0.75)));
            return mItems;
        }
        Menu Nactivate() {
            mPeriscope.Nactivate();
            return this;
        }
        Menu moduleScan() {

            var entity = new MyDetectedEntityInfo();
            ThyDetectedEntityInfo thy;
            var pos = mPeriscope.mCamera.WorldMatrix.Translation + mPeriscope.mCamera.WorldMatrix.Forward * mPeriscope.Range;
            if (mCamera.Scan(ref pos, ref entity, out thy)) {
                if (entity.Type == MyDetectedEntityType.None) {
                    mLog.persist("Module scan empty.");
                } else {
                    mLog.persist(mLog.gps(entity.Name, entity.Position));
                    mLog.persist("Module scan success.");
                }
            } else {
                mLog.persist("Module scan failed.");
            }

            return this;
        }
        Menu planetScan() {
            var orange = mPeriscope.Range;
            try {
                mPeriscope.Range = 6000000;
                scan();
            } finally {
                mPeriscope.Range = orange;
            }
            return this;
        }
        Menu scan() {
            if (mPeriscope.mCamera.AvailableScanRange <= mPeriscope.Range) {
                mLog.persist("Camera charging " + mPeriscope.mCamera.TimeUntilScan(mPeriscope.Range) / 1000 + " seconds remaining.");
            } else {
                MyDetectedEntityInfo entity = mPeriscope.mCamera.Raycast(mPeriscope.Range);
                if (entity.Type == MyDetectedEntityType.None) {
                    mLog.persist("Periscope scanned nothing.");
                } else {
                    if (entity.Type == MyDetectedEntityType.None) {
                        mLog.persist("Periscope scan empty.");
                    } else if (entity.EntityId == mManager.mProgram.Me.CubeGrid.EntityId) {
                        mLog.persist("Periscope scan obstructed by grid.");
                    } else {
                        
                        var mod = mCamera;
                        ThyDetectedEntityInfo thy;
                        if (mod == null) {
                            mLog.persist("Camera module interface failure.");
                        } else {
                            mLog.persist("Periscope scan success.");
                            mod.AddNew(entity, out thy);
                            if (thy == null) {
                                mLog.persist(mLog.gps(entity.Name, entity.Position));
                            } else {
                                mLog.persist(mLog.gps(thy.Name, thy.Position));
                            }
                        }
                    }
                }
            }
            return this;
        }
        Func<Menu> range(double fact) => () => { 
            mPeriscope.Range *= fact; 
            if (mPeriscope.Range < 1000) mPeriscope.Range = 1000; 
            return this;
        };
    }
}
