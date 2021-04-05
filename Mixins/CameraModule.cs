using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class CameraModule : Module<IMyCameraBlock>
    {

        int mClusterI = 0;
        MyDetectedEntityInfo? mCurrentIncoming;

        CameraList mCameraList;
        readonly Dictionary<long, long> mClusterLookup = new Dictionary<long, long>();
        readonly Dictionary<long, ThyDetectedEntityInfo> mLookup = new Dictionary<long, ThyDetectedEntityInfo>();
        readonly List<ThyDetectedEntityInfo> mDetected = new List<ThyDetectedEntityInfo>();
        public bool hasCamera => Blocks.Count > 0;
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();

        // todo organize camera by direction
        public CameraModule(ModuleManager aManager) : base(aManager) {
            MenuName = "Camera Records";
            onSave = SaveAction;
            onLoad = LoadAction;
            onPage = PageAction;
            onUpdate = ClusterAction;
        }
        public ThyDetectedEntityInfo Find(long entityId) {
            ThyDetectedEntityInfo result;
            long fkey;
            while (mClusterLookup.TryGetValue(entityId, out fkey)) {
                entityId = fkey;
            }
            mLookup.TryGetValue(entityId, out result);
            return result;
        }
        void ClusterAction() {
            if (mCurrentIncoming == null) {
                if (mIncoming.Count > 0) {
                    mClusterI = mDetected.Count - 1;
                    mCurrentIncoming = mIncoming.Dequeue();
                    var thy = Find(mCurrentIncoming.Value.EntityId);
                    if (thy == null) {
                        cluster();
                    } else {
                        thy.Seen(mCurrentIncoming.Value);
                        mCurrentIncoming = null;
                    }
                } else {
                    //logger.persist("ClusterAction nulling onUpdate");
                    onUpdate = null;
                }
            } else {
                mClusterI--;
                if (mClusterI < 0) {
                    var thy = new ThyDetectedEntityInfo(mCurrentIncoming.Value);
                    mDetected.Add(thy);
                    mLookup.Add(mCurrentIncoming.Value.EntityId, thy);
                    mCurrentIncoming = null;
                } else {
                    cluster();
                }
            }
        }
        
        void cluster() {
            var incomingIsClusterable = mCurrentIncoming.Value.Type == MyDetectedEntityType.Asteroid;
            //logger.persist($"incoming is clusterable {incomingIsClusterable}");
            if (incomingIsClusterable && mDetected.Count > 0) {
                var target = mDetected[mClusterI];
                //logger.persist($"target is clusterable {target.IsClusterable}");
                if (target.IsClusterable) {
                    var incomingWV = new BoundingSphereD(mCurrentIncoming.Value.HitPosition.Value, 1.0);
                    var sqdist = (target.WorldVolume.Center - incomingWV.Center).LengthSquared();
                    //logger.persist($"sqdist {sqdist}");
                    if (sqdist < 1048576) {
                        //logger.persist($"adding to cluster");
                        target.Cluster(incomingWV);
                        mClusterLookup[mCurrentIncoming.Value.EntityId] = target.EntityId;
                        mCurrentIncoming = null;
                    }
                }
            } else {
                var thy = new ThyDetectedEntityInfo(mCurrentIncoming.Value);
                mDetected.Add(thy);
                mLookup.Add(thy.EntityId, thy);
                mCurrentIncoming = null;
            }
        }

        void SaveAction(Serialize s) {
            var one = false;
            foreach (var e in mDetected) {
                if (one) {
                    s.rec();
                }
                s.unt("Record");
                s.str(e);
                one = true;
                foreach (var o in e.mOres) {
                    s.rec();
                    s.unt("Ore");
                    s.str(e.EntityId);
                    s.str(o.Name);
                    s.str(o.Position);
                }
            }
            foreach(var p in mClusterLookup) {
                if (one) {
                    s.rec();
                }
                s.unt("Cluster");
                s.str(p.Key);
                s.str(p.Value);
                one = true;
            }
        }

        void LoadAction(Serialize s, string aData) {
            var ar = aData.Split(Serialize.RECSEP);
            foreach (var record in ar) {
                var entry = record.Split(Serialize.UNTSEP);
                if (entry[0] == "Record") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        IEnumerable<string> elements = entries;

                        using (var en = elements.GetEnumerator()) {
                            en.MoveNext();
                            var thy = s.objThyDetectedEntityInfo(en);
                            mDetected.Add(thy);
                            mLookup.Add(thy.EntityId, thy);
                        }
                    }
                } else if (entry[0] == "Cluster") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        mClusterLookup[s.objlong(entries[0])] = s.objlong(entries[1]);
                    }
                } else if (entry[0] == "Ore") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        IEnumerable<string> elements = entries;
                        using (var en = elements.GetEnumerator()) {
                            en.MoveNext();
                            var id = s.objlong(en);
                            var name = s.objstring(en);
                            var pos = s.objVector3D(en);
                            var v3l = new Vector3L((long)pos.X, (long)pos.Y, (long)pos.Z);
                            ThyDetectedEntityInfo thy;
                            if (mLookup.TryGetValue(id, out thy)) {
                                thy.AddOre(new ThyDetectedEntityInfo.Ore(thy, name, v3l));
                            }
                        }
                    }
                }
            }
            if (mDetected.Count == 0) {
                mLookup.Clear();
                mClusterLookup.Clear();
            }
        }


        List<MenuItem> PageAction(int aPage) {
            aPage = Menu.PageIndex(aPage, mDetected.Count);
            int index = (mDetected.Count - 1) - (aPage * 6);
            int count = 0;
            mMenuItems.Clear();
            //logger.persist($"CameraModule.MenuMethods({aPage});");
            //logger.persist($"index={index}");
            //logger.persist($"mDetected.Count={mDetected.Count}");
            while (index >= 0 && count < 6) {
                var e = mDetected[index];
                var name = e.Name;
                if (e.Type == ThyDetectedEntityType.Asteroid) {
                    name = $"{e.Name} Asteroid";
                } else if (e.Type == ThyDetectedEntityType.AsteroidCluster) {
                    name = $"{e.Name} Cluster";
                }
                mMenuItems.Add(new MenuItem(name, e, EntityMenu));
                index--;
                count++;
            }
            return mMenuItems;
        }

        readonly Queue<MyDetectedEntityInfo> mIncoming = new Queue<MyDetectedEntityInfo>();

        public void AddNew(MyDetectedEntityInfo aEntity, out ThyDetectedEntityInfo thy) {
            if (aEntity.EntityId == 0 || aEntity.Type == MyDetectedEntityType.FloatingObject) {
                thy = null;
                return;
            }

            thy = Find(aEntity.EntityId);
            if (thy == null) {
                mIncoming.Enqueue(aEntity); 
                //logger.persist("CameraModule.AddNew - new unrecognized entity added to queue");
                if (onUpdate == null) {
                    onUpdate = ClusterAction;
                }
            } else {
                
                thy.Seen(aEntity);
                //logger.persist($"CameraModule.AddNew - updating {thy.Name}");
            }
        }

        const double HR = 3600000;
        bool deleted;

        Menu OreMenu(MenuModule aMain, object aState) {
            var thy = (ThyDetectedEntityInfo)aState;
            return new Menu(aMain, $"Ore Record for {thy.Name}", pg => {
                var page = Menu.PageIndex(pg, thy.mOres.Count);
                int index = pg * 6;
                int count = 0;
                mMenuItems.Clear();
                while (count < 6 && index < thy.mOres.Count) {
                    var o = thy.mOres[index];
                    mMenuItems.Add(new MenuItem(o.Name, o, (m, state) => {
                        var ore = (ThyDetectedEntityInfo.Ore)state;
                        return new Menu(aMain, $"Actions for {o.Name}", p => {
                            mMenuItems.Clear();
                            mMenuItems.Add(new MenuItem("Send Drill Drone", state, sendDrillDrone));
                            mMenuItems.Add(new MenuItem(logger.gps(ore.Name, ore.Position)));
                            mMenuItems.Add(new MenuItem("Veryify", state, verifyOre));
                            return mMenuItems;
                        });
                    }));
                    index++;
                    count++;
                }
                return mMenuItems;
            });
        }
        Menu verifyOre(MenuModule aMain, object state) {
            return null;
        }
        Menu sendDrillDrone(MenuModule aMain, object state) {
            var ore = (ThyDetectedEntityInfo.Ore)state;
            ATCModule atc;
            if (GetModule(out atc)) {
                
                if (atc.SendDrill(ore)) {
                    logger.persist(logger.gps(ore.Name, ore.Position));
                } else {
                    logger.persist("Drill not sent.");
                }
            }
            return null;
        }
        Menu EntityMenu(MenuModule aMain, object aState) {
            
            var e = (ThyDetectedEntityInfo)aState;
            
            deleted = false;

            return new Menu(aMain, $"Camera Record for {e.Name} {e.EntityId}", p => {
                p = p % 2;
                mMenuItems.Clear();
                if (deleted) {
                    aMain.Input("6");
                    return null;
                }
                if (deleted || p == 0) {
                    var ts = (DateTime.Now - e.TimeStamp).TotalHours;
                    mMenuItems.Add(new MenuItem($"Time: {e.TimeStamp} ({ts:f2} hours ago)"));
                    mMenuItems.Add(new MenuItem($"Relationship: {e.Relationship} - Type: {e.Type}"));
                    mMenuItems.Add(new MenuItem(logger.gps($"{e.Name}", e.Position)));
                    mMenuItems.Add(new MenuItem($"Distance: {(e.Position - MyMatrix.Translation).Length():f0} - Radius: {e.WorldVolume.Radius}"));
                    mMenuItems.Add(new MenuItem("Designate Target", () => controller.NewMission(new OrbitMission(controller, e))));
                    if (!deleted) {
                        mMenuItems.Add(new MenuItem($"Rename to '{mManager.UserInput}'", aState, renameRecord));
                    }
                } else {
                    if (e.mOreTypes.Count > 0) {
                        mMenuItems.Add(new MenuItem("Ores: " + string.Join(", ", e.mOreTypes), e, OreMenu));
                    }
                    mMenuItems.Add(new MenuItem("Delete Record", aState, deleteRecordConfirm));
                }
                
                return mMenuItems;
            });
        }
        Menu renameRecord(MenuModule aMain, object aState) {
            ((ThyDetectedEntityInfo)aState).SetName(mManager.UserInput);
            return null;
        }
        Menu deleteRecordConfirm(MenuModule aMain, object aState) {

            if (mIncoming.Count > 0) {
                logger.persist($"Clustering in process please wait to delete.");
            } else {
                if (deleted) {
                    aMain.Input("6");
                } else {
                    return new Menu(aMain, "Confirm Deletion", p => {
                        var list = new List<MenuItem>();
                        list.Add(new MenuItem("Yes", aState, deleteRecord));
                        return list;
                    });
                }
            }
            return null;

        }
        Menu deleteRecord(MenuModule aMain, object aState) {
            if (mIncoming.Count > 0) {
                logger.persist($"Clustering in process please wait to delete."); 
            } else {
                aMain.Input("6");
                var thy = aState as ThyDetectedEntityInfo;
                deleted = mDetected.Remove(thy);
                mLookup.Remove(thy.EntityId);
                mClusterLookup.Remove(thy.EntityId);
                logger.persist($"Deleted: {deleted}");
            }
            return null;

        }
        /*Menu EntityGPS(MenuModule aMain, object aState) {
            var e = (MyDetectedEntityInfo)aState;
            logger.persist(logger.gps(e.Name, e.HitPosition.Value));
            return null;
        }*/
        
        Menu AimGyro(MenuModule aMain, object aState) {
            GyroModule mod;
            if (GetModule(out mod)) {
                mod.SetTargetPosition((Vector3D)aState);
            }
            return null;
            
        }


        public override bool Accept(IMyTerminalBlock aBlock) {
            bool result = false; 
            if (aBlock is IMyMotorStator) {
                var rotor = aBlock as IMyMotorStator;
                if (mManager.HasTag(rotor, "camera")) {
                    rotor.Enabled = true;
                    rotor.RotorLock = false;
                    rotor.BrakingTorque = 0;
                    rotor.Torque = 1000.0f;
                    rotor.TargetVelocityRad = 1.0f;
                }
                result = true;
            } else if (aBlock.CubeGrid == mManager.mProgram.Me.CubeGrid) {
                result = base.Accept(aBlock);
                if (result) {

                    if (mCameraList == null) {
                        mCameraList = new CameraList(this);
                    }
                    var camera = aBlock as IMyCameraBlock;
                    
                    mCameraList.Add(camera);
                    //logger.persist(camera.CustomName);
                    camera.ShowInTerminal = false;
                    camera.Enabled = true;
                    camera.EnableRaycast = true;
                }
            }
            return result;
        }
        /// <summary>
        /// returns true if we found a camera that will scan the target location and scanned it
        /// </summary>
        /// <param name="aTarget"></param>
        /// <param name="aAddDistance"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool Scan(Vector3D aTarget, out MyDetectedEntityInfo entity, out ThyDetectedEntityInfo thy, double aAddDist = 0) {
            if (mCameraList.Scan(aTarget, out entity, aAddDist)) {
                if (entity.EntityId != 0) {
                    AddNew(entity, out thy);
                } else {
                    thy = null;
                }
                return true;
            }
            thy = null;
            entity = default(MyDetectedEntityInfo);
            return false;
        }

        /*
         TransformNormal == Rotate
         Exact same code
        */
        /// <summary>
        /// whiplash
        /// https://discord.com/channels/125011928711036928/216219467959500800/810367147489361960
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        bool testCameraAnglesAE(IMyCameraBlock aCamera, Vector3D aDirection, ref double aYawTan, ref double aPitchTan1Sq, ref double aPitchTan2Sq) {
            aDirection = Vector3D.Rotate(aDirection, MatrixD.Transpose(aCamera.WorldMatrix));

            if (aDirection.Z > 0) {
                return false;
            }
            aYawTan = aDirection.X / aDirection.Z;
            aPitchTan1Sq = aDirection.X * aDirection.X;
            aPitchTan2Sq = aDirection.Z * aDirection.Z;
            var pitchTanSq = aDirection.Y * aDirection.Y / (aPitchTan1Sq + aPitchTan2Sq);

            return Math.Abs(aYawTan) <= 1 && pitchTanSq <= 1;
        }
        bool testCameraAngles(IMyCameraBlock camera, ref Vector3D aDirection) {
            aDirection = Vector3D.Rotate(aDirection, MatrixD.Transpose(camera.WorldMatrix));

            if (aDirection.Z > 0) //pointing backwards
                return false;

            var yawTan = Math.Abs(aDirection.X / aDirection.Z);
            var pitchTanSq = aDirection.Y * aDirection.Y / (aDirection.X * aDirection.X + aDirection.Z * aDirection.Z);

            return yawTan <= 1 && pitchTanSq <= 1;
            
        }
        bool zTestCameraAngles(IMyCameraBlock camera, Vector3D direction) {
            Vector3D localDirection = Vector3D.Rotate(direction, MatrixD.Transpose(camera.WorldMatrix));

            if (localDirection.Z > 0) //pointing backwards
                return false;

            var yawTan = Math.Abs(localDirection.X / localDirection.Z);
            var pitchTan = Math.Abs(localDirection.Y / localDirection.Z);

            return yawTan <= 1 && pitchTan <= 1;
        }
        /*
        /// Whip's Get Rotation Angles Method v14 - 9/25/18 ///
        MODIFIED FOR WHAM FIRE SCRIPT 2/17/19
        Dependencies: AngleBetween
        * /
        void GetRotationAngles(Vector3D targetVector, MatrixD worldMatrix, out double yaw, out double pitch) {
            var localTargetVector = Vector3D.TransformNormal(targetVector, MatrixD.Transpose(worldMatrix));
            var flattenedTargetVector = new Vector3D(localTargetVector.X, 0, localTargetVector.Z);

            yaw = AngleBetween(Vector3D.Forward, flattenedTargetVector) * Math.Sign(localTargetVector.X); //right is positive
            if (Math.Abs(yaw) < 1E-6 && localTargetVector.Z > 0) //check for straight back case
                yaw = Math.PI;

            if (Vector3D.IsZero(flattenedTargetVector)) //check for straight up case
                pitch = MathHelper.PiOver2 * Math.Sign(localTargetVector.Y);
            else
                pitch = AngleBetween(localTargetVector, flattenedTargetVector) * Math.Sign(localTargetVector.Y); //up is positive
        }//*/
    }
}
