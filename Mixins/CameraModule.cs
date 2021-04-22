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
        const double HR = 3600000;
        int mClusterI = 0;
        MyDetectedEntityInfo? mCurrentIncoming;

        CameraList mCameraList;
        public readonly Dictionary<long, long> mClusterLookup = new Dictionary<long, long>();
        public readonly Dictionary<long, ThyDetectedEntityInfo> mLookup = new Dictionary<long, ThyDetectedEntityInfo>();
        public readonly List<ThyDetectedEntityInfo> mDetected = new List<ThyDetectedEntityInfo>();
        public bool hasCamera => Blocks.Count > 0;
        

        // todo organize camera by direction
        public CameraModule(ModuleManager aManager) : base(aManager) {
            //onSave = SaveAction;
            //onLoad = LoadAction;
            onUpdate = ClusterAction;
            Active = true;
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
        public void Sort() {
            if (onUpdate == null) {
                mDetected.Sort((a, b) => b.TimeStamp.CompareTo(a.TimeStamp));
            }
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
                    Sort();
                }
            } else {
                mClusterI--;
                if (mClusterI < 0) {
                    var thy = new ThyDetectedEntityInfo(mCurrentIncoming.Value);
                    if (mCurrentIncoming.Value.Type == MyDetectedEntityType.Asteroid) {
                        mManager.mMachines.Enqueue(thy.asteroidIdentifier(this));
                    }
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
                //logger.persist($"target is clusterable {target.IsClusterable}");
                if (target.IsClusterable) {
                    var incomingWV = new BoundingSphereD(mCurrentIncoming.Value.HitPosition.Value, 1.0);
                    var sqdist = (target.WorldVolume.Center - incomingWV.Center).LengthSquared();
                    //logger.persist($"sqdist {sqdist}");
                    if (sqdist < 2359296) {
                        //logger.persist($"adding to cluster");
                        target.Cluster(incomingWV);
                        mClusterLookup[mCurrentIncoming.Value.EntityId] = target.EntityId;
                        mCurrentIncoming = null;
                    }
                }
            } else {
                var thy = new ThyDetectedEntityInfo(mCurrentIncoming.Value);
                if (mCurrentIncoming.Value.Type == MyDetectedEntityType.Asteroid) {
                    mManager.mMachines.Enqueue(thy.asteroidIdentifier(this));
                }
                mDetected.Add(thy);
                mLookup.Add(thy.EntityId, thy);
                mCurrentIncoming = null;
            }
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

        
        public void DeleteRecord(ThyDetectedEntityInfo aEntity) {
            if (onUpdate == null) {
                mDetected.Remove(aEntity);
                mLookup.Remove(aEntity.EntityId);
                mClusterLookup.Remove(aEntity.EntityId);
                mLog.persist("Deleted");
            } else {
                mLog.persist($"Clustering in process please wait to delete.");
            }
        }
        /*Menu EntityGPS(MenuModule aMain, object aState) {
            var e = (MyDetectedEntityInfo)aState;
            logger.persist(logger.gps(e.Name, e.HitPosition.Value));
            return null;
        }*/
        
        /*Menu AimGyro(MenuModule aMain, object aState) {
            GyroModule mod;
            if (GetModule(out mod)) {
                mod.SetTargetPosition((Vector3D)aState);
            }
            return null;
            
        }*/


        public override bool Accept(IMyTerminalBlock aBlock) {
            bool result = false; 
            if (aBlock is IMyMotorStator) {
                var rotor = aBlock as IMyMotorStator;
                if (mManager.hasTag(rotor, "camera")) {
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
