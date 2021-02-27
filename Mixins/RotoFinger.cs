using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    class RotoFinger
    {
        Vector3D mvTarget;
        float mfTarget;
        FingerMode mode;
        // orange #EE4C00
        // blue #085FA6
        // red #B20909
        // yellow #E8B323   
        /// <summary>
        /// bottom of finger handles yaw
        /// </summary>
        public readonly Stator stator;
        /// <summary>
        /// top of finger handles pitch
        /// </summary>
        public readonly Stator hinge;

        
        IMyExtendedPistonBase piston;
        public IMyEntity tip => hinge == null ? null : hinge.Top;
        readonly Logger g;
        readonly GTS gts;

        public readonly RotoFinger firstFinger;

        RotoFinger _nextFinger;
        public RotoFinger nextFinger {
            get {
                if (_nextFinger == null) {
                    _nextFinger = next();
                }
                return _nextFinger;
            }
        }
        
        public readonly RotoFinger previousFinger;
        
        

        //readonly float hingeOffset = MathHelper.Pi;
        //float statorOffset;
        //float parentHingeOffset = 0;
        //float parentStatorOffset = 0;

        //float hingeValue, statorValue;
        //bool hingeLocked, statorLocked;
        
        public readonly int Identity = 0;

        //const float torque = 1000000000f;
        //const float hingeLimit = 1.570796f;
        const float epsilon = 1.0f * (MathHelper.Pi / 180.0f);

        const float factorMax = 0.2f;
        const float factorMin = 0.1f;

        enum FingerMode {
            hold,
            point
        }


        public bool okay {
            get; private set;
        }
        public bool stopped { get; private set; }
        
        public void Stop() {
            
            stator.Stop();
            hinge.Stop();
            stopped = true;
        }
        public void Go() {
            stator.Go();
            hinge.Go();
            stopped = false;
        }
        public RotoFinger(IMyMotorAdvancedStator aStator, Logger aLogger, GTS aGTS, RotoFinger aPrevious = null) {
            if (aStator == null || aLogger == null || aGTS == null || aStator.Top == null) {
                return;
            }

            stator = new Stator(aStator, aLogger);
            stator.reverse = true;
            g = aLogger;
            gts = aGTS;
            
            if (aPrevious != null) {
                previousFinger = aPrevious;
                Identity = aPrevious.Identity + 1;
            }

            aStator.CustomName = "Finger - " + Identity.ToString("D2") + " - Turn";
            try {
                IMyMotorAdvancedStator hingeRotor = null;
                gts.getByGrid(aStator.TopGrid.EntityId, ref hingeRotor);
                if (hingeRotor != null) {
                    hinge = new Stator(hingeRotor, aLogger);
                    okay = true;
                    if (hinge.TopGrid != null) {
                        gts.getByGrid(hinge.TopGrid.EntityId, ref piston);
                    }
                    bool near;
                    float ab;
                    var dot = double.NaN;
                    var hingeUpDir = hinge.stator.Orientation.Up;

                    var statorTopLeft = Base6Directions.GetVector(stator.Top.Orientation.Left);
                    var statorTopBack = -Base6Directions.GetVector(stator.Top.Orientation.Forward);
                    var hingeUp = Base6Directions.GetVector(hingeUpDir);
                    ab = (float)MAF.angleBetween(hingeUp, statorTopLeft);
                    near = MAF.nearEqual(ab, MathHelper.PiOver2);

                    if (near) {
                        dot = hingeUp.Dot(statorTopBack);
                        if (dot < 0) {
                            ab = -ab;
                        }
                    }

                    stator.mfOffset += ab;
                    stator.stator.CustomName += " ab:" + ab.ToString("f2") + " d:" + dot.ToString("f2");

                    if (previousFinger != null) {
                        //stator.mfOffset -= previousFinger.stator.mfOffset;
                        // angle between previous.hinge.top.up and this.stator.back
                        var previousHingeTopUp = Base6Directions.GetVector(previousFinger.hinge.stator.Top.Orientation.Up);
                        var statorFront = Base6Directions.GetVector(stator.stator.Orientation.Forward);
                        var statorLeft = Base6Directions.GetVector(stator.stator.Orientation.Left);
                        ab = (float)MAF.angleBetween(previousHingeTopUp, statorLeft);
                        near = MAF.nearEqual(ab, MathHelper.PiOver2);
                        if (near) {
                            dot = previousHingeTopUp.Dot(statorFront);
                            if (dot < 0) {
                                ab = -ab;
                            }
                        }
                        stator.mfOffset += ab;
                        g.persist(Identity + " ab phtu & sb " + ab + " dot:" + dot);

                    }

                    //}
                    hingeRotor.CustomName = "Finger - " + Identity.ToString("D2") + " - Bend";

                    // var statorFront = aStator.Orientation.Forward;
                    // var statorUp = aStator.Orientation.Up;

                    // offset the hinge from the stator

                    if (aStator.Top != null) {
                        var statorTopUp = Base6Directions.GetVector(aStator.Top.Orientation.Up);
                        var hingeBack = -Base6Directions.GetVector(hinge.stator.Orientation.Forward);
                        var hingeLeft = Base6Directions.GetVector(hinge.stator.Orientation.Left);

                        ab = (float)MAF.angleBetween(statorTopUp, hingeBack);
                        dot = double.NaN;
                        near = MAF.nearEqual(ab, MathHelper.PiOver2);

                        if (near) {
                            dot = statorTopUp.Dot(hingeLeft);
                            if (dot < 0) {
                                ab = -ab;
                            }
                        }

                        hinge.stator.CustomName += " c:" + ab.ToString("f2") + " d:" + dot.ToString("f2");
                        hinge.mfOffset += ab;
                    }

                }
            } catch(Exception ex) {
                g.persist("Finger initialization FAILED: " + ex.ToString());
            }
        }
        bool hingeClose, statorClose;
        public bool close => hingeClose && statorClose;
        RotoFinger next() {
            RotoFinger result = null;
            if (okay) {
                var topGrid = hinge.TopGrid;
                if (topGrid != null) {
                    
                    IMyMotorAdvancedStator rotor = null;
                    if (piston == null || piston.TopGrid == null) {
                        gts.getByGrid(hinge.TopGrid.EntityId, ref rotor);
                    } else {
                        gts.getByGrid(piston.TopGrid.EntityId, ref rotor);
                    }

                    if (rotor != null) {
                        result = new RotoFinger(rotor, g, gts, this);
                        

                        // https://math.stackexchange.com/a/7934
                        // If v is the vector that points 'up' and p0 is some point on your plane, and finally p is the point that 
                        // might be below the plane, compute the dot product v⋅(p−p0). This projects the vector to p on the 
                        // up-direction. This product is {−,0,+} if p is below, on, above the plane, respectively.
                        var hingeTopRight = -Base6Directions.GetVector(hinge.Top.Orientation.Left);
                        var hingeTopBack = -Base6Directions.GetVector(hinge.Top.Orientation.Forward);
                        IMyTerminalBlock block = piston;
                        if (block == null) {
                            block = result.stator.stator;
                        }
                        var nextUp = Base6Directions.GetVector(block.Orientation.Up);
                        var dot = double.NaN;
                        var ab = MAF.angleBetween(nextUp, hingeTopBack);
                        
                        if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                            dot = nextUp.Dot(hingeTopRight);
                            if (dot < 0) {
                                ab = -ab;
                            }
                        }
                        hinge.mfOffset += (float)ab;

                        hinge.stator.CustomName += " n:" + ab.ToString("f2") + " d:" + dot.ToString("f2");
                        if (piston != null) {
                            piston.CustomName = "Finger - " + result.Identity.ToString("D2");
                        }
                    }
                }
            }
            return result;
        }
        public void Update() {
            g.log(Identity + " " + stator.mfOffset);
            stator.Update();
            hinge.Update();
        }

        public void SetTarget(float aTarget) {
            mfTarget = aTarget;
            mode = FingerMode.hold;
            stator.SetTarget(aTarget);
            hinge.SetTarget(aTarget);
            stator.reverse = false;
        }
  
        public void SetTarget(Vector3D aTarget) {
            mvTarget = aTarget;
            mode = FingerMode.point;
            stator.SetTarget(aTarget);
            hinge.SetTarget(aTarget);
            stator.reverse = true;
            
            /*if (stopped) {
                setHingeAngle(hingeStop);
                setStatorAngle(0, false);
            } else {
                var targetVector = Vector3D.Normalize(aTarget - stator.WorldMatrix.Translation);
                var local = MAF.world2dir(targetVector, stator.WorldMatrix);
                var localFlat = new Vector3D(local.X, 0, local.Z);

                var ab = MAF.angleBetween(Vector3D.Left, localFlat);

                if (local.Z < 0) {
                    ab -= MathHelper.PiOver2;
                } else {
                    ab += MathHelper.PiOver2;
                    ab = -ab;
                }

                setStatorAngle((float)ab, false);

                ab = MAF.angleBetween(Vector3D.Up, local);
                setHingeAngle(-(float)ab);
                if (close && (parent == null || parent.stopped)) {
                    stop();
                }
            }*/
        }
        /*void setStatorAngle(float aAngle, bool offset = true) {
            if (stopped && aAngle != float.MaxValue) {
                aAngle = statorStop - stator.Angle;
            } else {
                if (offset) {
                    if (aAngle == float.MaxValue) {
                        aAngle = 0;
                    }
                    //var phOffset = parent == null ? 0f : parent.hingeOffset;
                    //var psOffset = parent == null ? 0f : parent.statorOffset;
                    //aAngle = (aAngle + hingeOffset - statorOffset) - stator.Angle;
                    aAngle = (aAngle - statorOffset) - stator.Angle;
                } else {
                    //aAngle = (aAngle + hingeOffset) - stator.Angle;
                    aAngle = aAngle  - stator.Angle;
                }

                MathHelper.LimitRadians(ref aAngle);
                if (aAngle > MathHelper.Pi) {
                    aAngle -= MathHelper.TwoPi;
                } else if (aAngle < -MathHelper.Pi) {
                    aAngle += MathHelper.TwoPi;
                }
            }
            stator.setTarget(aAngle);
        }*/
        

        public void Info() {
            g.log("Finger " + Identity);
            //g.log("Stator");
            stator.Info();
            //g.log("Hinge");
            //hinge.Info();
        }
        // aAngle = 4
        // angle = 5
        // -1 = 4 - 5

        // aAngle = 5
        // angle = 4
        // 1 = 5 - 4

        // aAngle = 5
        // angle = 2
        // 3 = 5 - 2

        // aAngle = 5
        // angle = 1
        // 4 = 5 - 1

        // aAngle = 3
        // angle = 1
        // 2 = 3 - 1
        // angle = aAngle - angle;

        // aAngle = 2
        // angle = 1
        // 1 = 2 - 1
        // angle = aAngle - angle;

        // aAngle = 1
        // angle = 2
        // -1 = 1 - 2
        // angle = aAngle - angle;

        // aAngle = 1
        // angle = 5
        // -4 = 1 - 5
        // angle = aAngle - angle;

        // aAngle = 1
        // angle = 6
        // -5 = 1 - 6
        // angle = aAngle - angle;

        // aAngle 0
        // angle = twopi
        // twopi = 0 - twopi

        // aAngle = 0
        // angle = 6.28
        // 6.28 = 0 - angle

        // aAngle = 0
        // angle = 6.28
        // 6.28 = 0 - angle
        /*
        
        void pointRotoAtTarget(IMyMotorStator aRoto, Vector3D aTarget) {
            // cos(angle) = dot(vecA, vecB) / (len(vecA)*len(vecB))
            // angle = acos(dot(vecA, vecB) / (len(vecA)*len(vecB)))
            // angle = acos(dot(vecA, vecB) / sqrt(lenSq(vecA)*lenSq(vecB)))
            if (aTarget == Vector3D.Zero) {
                pointRotoAtDirection(aRoto, aTarget);
            } else {
                var matrix = aRoto.WorldMatrix;
                var projectedTarget = aTarget - Vector3D.Dot(aTarget - matrix.Translation, matrix.Up) * matrix.Up;
                var projectedDirection = Vector3D.Normalize(matrix.Translation - projectedTarget);
                pointRotoAtDirection(aRoto, projectedDirection);
            }
        }
        void pointRotoAtDirection(IMyMotorStator aRoto, Vector3D aDirection) {
            if (null != aRoto) {
                if (Vector3D.Zero == aDirection) {
                    aRoto.TargetVelocityRad = 0;
                    return;
                }
                var matrix = aRoto.WorldMatrix;
                double dot;

                var angle = MAF.angleBetween(aDirection, matrix.Forward, out dot);
                //log("roto angle ", angle);
                double targetAngle;
                var v = 0.0;
                if (!double.IsNaN(angle)) {
                    // norm = dir to me cross grav
                    // dot = dir to obj dot norm
                    var norm = aDirection.Cross(matrix.Forward);

                    dot = matrix.Up.Dot(norm);
                    if (dot < 0) {
                        targetAngle = (Math.PI * 2) - angle;
                    } else {
                        targetAngle = angle;
                    }
                    v = targetAngle - aRoto.Angle;
                    if (v > Math.PI) {
                        v -= (Math.PI * 2);
                    }
                    if (v < -Math.PI) {
                        v += (Math.PI * 2);
                    }
                }
                aRoto.Enabled = true;
                aRoto.RotorLock = false;
                var max = 0.2;
                aRoto.TargetVelocityRad = (float)MathHelper.Clamp(v, -max, max);
            }
        }*/
    }
}
