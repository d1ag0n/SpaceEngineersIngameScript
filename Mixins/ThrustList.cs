using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    
    class ThrustList {
        //public Vector3D Acceleration;
        // these provide acceleration in the respective direction
        readonly List<IMyThrust> mLeft = new List<IMyThrust>();
        readonly List<IMyThrust> mRight = new List<IMyThrust>();
        readonly List<IMyThrust> mUp = new List<IMyThrust>();
        readonly List<IMyThrust> mDown = new List<IMyThrust>();
        readonly List<IMyThrust> mFront = new List<IMyThrust>();
        readonly List<IMyThrust> mBack = new List<IMyThrust>();
        
        public void Add(IMyShipController aController, IMyThrust aThrust) {
            var o = aController.Orientation;
            var f = aThrust.Orientation.Forward;

            if (f == o.Forward) {
                mBack.Add(aThrust);
            } else if (f == o.Up) {
                mDown.Add(aThrust);
            } else if (f == o.Left) {
                mRight.Add(aThrust);
            } else if (f == Base6Directions.GetOppositeDirection(o.Forward)) {
                mFront.Add(aThrust);
            } else if (f == Base6Directions.GetOppositeDirection(o.Up)) {
                mUp.Add(aThrust);
            } else if (f == Base6Directions.GetOppositeDirection(o.Left)) {
                mLeft.Add(aThrust);
            } else {
                throw new Exception($"WTF Direction {f}");
            }
        }
        public void Update(ref Vector3D aAccel, double aMass, bool emergency = false) {
            pickList(aMass, ref aAccel.X, mRight, mLeft, emergency);
            pickList(aMass, ref aAccel.Y, mDown, mUp, emergency);
            pickList(aMass, ref aAccel.Z, mFront, mBack, emergency);
        }

        void pickList(double aMass, ref double aAccel, List<IMyThrust> aNeg, List<IMyThrust> aPos, bool emergency) {
            var o = aNeg;
            if (aAccel > 0) {
                aNeg = aPos;
                aPos = o;
            }
            runList(aMass, ref aAccel, aNeg, emergency);
            foreach(var t in aPos) {
                if (t.Enabled) t.Enabled = false;
            }
        }

        void runList(double aMass, ref double aAccel, List<IMyThrust> aList, bool emergency) {
            var A = Math.Abs(aAccel);
            var F = aMass * A;
            //ModuleManager.logger.log($"F = {F}");
            //ModuleManager.logger.log($"M = {aMass}");
            //ModuleManager.logger.log($"A = {A}");
            
            foreach (var t in aList) {
                double met = t.MaxEffectiveThrust;
                if (A > 0 && (met / t.MaxThrust > 0.75 || (emergency && t.MaxEffectiveThrust > 0))) {
                    if (t.IsFunctional) {
                        if (!t.Enabled) t.Enabled = true;
                        // 2 < 1 = false;
                        if (met < F) {
                            t.ThrustOverridePercentage = 1;
                            F -= met;
                            if (aAccel > 0) {
                                aAccel -= met / aMass;
                            } else {
                                aAccel += met / aMass;
                            }
                        } else {
                            t.ThrustOverridePercentage = (float)(F / met);
                            F = 0;
                            aAccel = 0;
                        }
                    }
                } else {
                    if (t.Enabled) t.Enabled = false;
                    t.ThrustOverride = 0;
                }
            }
        }
    }
}
