using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class CameraModule : Module<IMyCameraBlock>
    {
        int clusterI = 0;
        
        readonly Dictionary<long, long> mClusterLookup = new Dictionary<long, long>();
        readonly Dictionary<long, ThyDetectedEntityInfo> mLookup = new Dictionary<long, ThyDetectedEntityInfo>();
        readonly List<ThyDetectedEntityInfo> mDetected = new List<ThyDetectedEntityInfo>();
        public bool hasCamera => Blocks.Count > 0;
        readonly List<MenuItem> mMenuItems = new List<MenuItem>();

        // todo organize camera by direction
        public CameraModule() {
            MenuName = "Camera Records";
            onSave = SaveAction;
            onLoad = LoadAction;
            onPage = PageAction;
        }
        void ClusterAction() {
            if (mIncoming.Count > 0) {
                var thy = mIncoming.Peek();
                if (thy.IsClusterable) {

                } else {

                }
            }
        }
        
        void cluster() {
            clusterY++;
            if (clusterY == mDetected.Count) {
                clusterY = 0;
                clusterX++;
                if (clusterX == mDetected.Count) {
                    clusteringComplete = true;
                    return;
                }
            }
            
            var thyx = mDetected[clusterX];
            if (thyx.IsClusterable) {
                var thy = mDetected[clusterY];
                if (thy.IsClusterable) {
                    var sqdist = (thyx.WorldVolume.Center - thy.WorldVolume.Center).LengthSquared();
                    var cluster = sqdist < 4000000;

                    if (!cluster) {
                        if (thyx.WorldVolume.Intersects(thyx.WorldVolume)) {
                            cluster = true;
                        }
                    }

                    if (cluster) {
                        if (thyx.WorldVolume.Radius > thy.WorldVolume.Radius) {
                            thyx.Cluster(thy);
                            mDetected.Remove(thy);
                            mClusterLookup[thy.EntityId] = thyx.EntityId;
                        } else {
                            thy.Cluster(thyx);
                            mDetected.Remove(thyx);
                            mClusterLookup[thyx.EntityId] = thy.EntityId;
                        }
                        clusterX = 0;
                        clusterY = -1;
                    }
                }
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
            ThyDetectedEntityInfo thy;
            while(mIncoming.TryDequeue(out thy)) {
                if (one) {
                    s.rec();
                }
                s.unt("Incoming");
                s.str(thy);
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
                        }
                    }
                } else if (entry[0] == "Cluster") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        mClusterLookup[s.objlong(entries[0])] = s.objlong(entries[1]);
                    }
                } else if (entry[0] == "Incoming") {
                    var entries = entry[1].Split(s.NL, StringSplitOptions.None);
                    if (entries.Length > 0) {
                        IEnumerable<string> elements = entries;
                        using (var en = elements.GetEnumerator()) {
                            en.MoveNext();
                            mIncoming.Enqueue(s.objThyDetectedEntityInfo(en));
                        }
                    }
                }
            }
            onUpdate = ClusterAction;
        }


        List<MenuItem> PageAction(int aPage) {
            aPage = Menu.PageNumber(aPage, mDetected.Count);
            int index = (mDetected.Count - 1) - (aPage * 6);
            int count = 0;
            mMenuItems.Clear();
            //logger.persist($"CameraModule.MenuMethods({aPage});");
            //logger.persist($"index={index}");
            //logger.persist($"mDetected.Count={mDetected.Count}");
            while (index >= 0 && count < 6) {
                var e = mDetected[index];
                mMenuItems.Add(new MenuItem(e.Type == ThyDetectedEntityType.Asteroid ? $"Asteroid {e.Name}" : e.Name, e, EntityMenu));
                index--;
                count++;
            }
            return mMenuItems;
        }

        readonly Queue<ThyDetectedEntityInfo> mIncoming = new Queue<ThyDetectedEntityInfo>();

        public void AddNew(MyDetectedEntityInfo aEntity) {
            ThyDetectedEntityInfo thy;

            if (mLookup.TryGetValue(aEntity.EntityId, out thy)) {
                thy.Seen();
                mDetected.Remove(thy);
                mDetected.Add(thy);
            } else if (mClusterLookup.ContainsKey(aEntity.EntityId)) {
                var key = aEntity.EntityId;
                long fkey;
                while (mClusterLookup.TryGetValue(key, out fkey)) {
                    key = fkey;
                }
                mLookup[key].Seen();
            } else {
                mIncoming.Enqueue(new ThyDetectedEntityInfo(aEntity));
            }
        }
        const double HR = 3600000;
        bool deleted;
        Menu EntityMenu(MenuModule aMain, object aState) {
            
            var e = (ThyDetectedEntityInfo)aState;
            
            deleted = false;

            return new Menu(aMain, $"Camera Record for {e.Name} {e.EntityId}", p => {
                p = p % 2;
                mMenuItems.Clear();
                if (deleted || p == 0) {
                    var ts = (MAF.time - e.TimeStamp) / HR;
                    mMenuItems.Add(new MenuItem($"Time: {ts:f2} hours ago"));
                    mMenuItems.Add(new MenuItem($"Relationship: {e.Relationship}"));
                    mMenuItems.Add(new MenuItem(logger.gps($"{e.Name}", e.Position)));
                    mMenuItems.Add(new MenuItem($"Distance: {(e.Position - Grid.WorldMatrix.Translation).Length():f0}"));
                    mMenuItems.Add(new MenuItem("Designate Target", () => controller.Target = e));
                    if (!deleted) {
                        mMenuItems.Add(new MenuItem($"Rename to '{ModuleManager.UserInput}'", aState, renameRecord));
                    }
                } else {
                    if (!deleted) {
                        mMenuItems.Add(new MenuItem("Delete Record", aState, deleteRecord));
                    }
                }
                
                return mMenuItems;
            });
        }
        Menu renameRecord(MenuModule aMain, object aState) {
            ((ThyDetectedEntityInfo)aState).SetName(ModuleManager.UserInput);
            return null;
        }
        Menu deleteRecord(MenuModule aMain, object aState) {
            if (clusteringComplete) {
                deleted = mDetected.Remove((ThyDetectedEntityInfo)aState);
                logger.persist($"Deleted: {deleted}");
            } else {
                logger.persist($"Clustering in process please wait to delete.");
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


        public override bool Accept(IMyCubeBlock aBlock) {
            if (aBlock is IMyMotorStator) {
                var rotor = aBlock as IMyMotorStator;
                if (ModuleManager.HasTag(rotor, "camera")) {
                    rotor.Enabled = true;
                    rotor.RotorLock = false;
                    rotor.BrakingTorque = 0;
                    rotor.Torque = 1000.0f;
                    rotor.TargetVelocityRad = 1.0f;
                }
                return true;
            }
            var result = base.Accept(aBlock);
            if (result) {
                var camera = aBlock as IMyCameraBlock;
                camera.Enabled = true;
                camera.EnableRaycast = true;
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
        public bool Scan(Vector3D aTarget, out MyDetectedEntityInfo aEntity, double aAddDist = 0) {
            bool rangeOkay = false;
            int t = int.MaxValue;
            foreach (var camera in Blocks) { 
                var dir = aTarget - camera.WorldMatrix.Translation;
                var dist = dir.Normalize() + aAddDist;

                double azimuth = 0, elevation = 0;

                if (camera.AvailableScanRange > dist) {
                    rangeOkay = true;
                    /*if (testCameraAngles(c, dir, ref yaw, ref pitch)) {
                        e = c.Raycast(dist, (float)pitch, (float)yaw);
                        return true;
                    }*/

                    if (testCameraAngles(camera, ref dir)) {
                        Vector3D.GetAzimuthAndElevation(dir, out azimuth, out elevation);
                        azimuth = -(azimuth * (180.0 / Math.PI));
                        elevation = (elevation * (180.0 / Math.PI));
                        aEntity = camera.Raycast(dist, (float)elevation, (float)azimuth);

                        if (aEntity.Type != MyDetectedEntityType.None) {
                            if (aEntity.EntityId == ModuleManager.Program.Me.CubeGrid.EntityId) {
                                continue;
                            }
                            AddNew(aEntity);
                            return true;
                        }
                    }
                } else {
                    var i = camera.TimeUntilScan(dist);
                    if (i < t) t = i;
                }
            }
            aEntity = default(MyDetectedEntityInfo);
            if (!rangeOkay) {
                logger.persist($"Camera charging {t / 1000} seconds remaining.");
            }
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
