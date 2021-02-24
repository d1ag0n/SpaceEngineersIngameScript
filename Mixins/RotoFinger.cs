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
        public readonly RotoFinger parent;
        //readonly float hingeOffset = MathHelper.Pi;
        //float statorOffset;
        //float parentHingeOffset = 0;
        //float parentStatorOffset = 0;

        //float hingeValue, statorValue;
        //bool hingeLocked, statorLocked;
        static int index = 0;
        public readonly int Identity;

        //const float torque = 1000000000f;
        //const float hingeLimit = 1.570796f;
        const float epsilon = 1.0f * (MathHelper.Pi / 180.0f);

        const float factorMax = 0.2f;
        const float factorMin = 0.1f;

        enum FingerMode {
            hold,
            point,
            crane
        }
        /*float factor { 
            get {
                return 0.1f;
                var range = factorMax - factorMin;
                var chunk = range / index;
                return factorMin + (chunk * (Identity + 1));
            }
        }*/

        public bool okay {
            get; private set;
        }
        public bool stopped { get; private set; }
        
        public void Stop() {
            g.persist("finger stopped");
            stator.Stop();
            hinge.Stop();
            stopped = true;
        }
        public void Go() {
            stator.Go();
            hinge.Go();
            stopped = false;
        }

        float mfHingeOffset = 0;
        float mfStatorOffset = 0;
        void genHingeOffset() {

        }

        /// <summary>
        /// generate offset for non primary fingers only
        /// </summary>
        /// <param name="statorUp"></param>
        /// <param name="statorFront"></param>
        void genStatorOffset(Base6Directions.Direction statorUp, Base6Directions.Direction statorFront) {
            switch (statorUp) {
                case Base6Directions.Direction.Backward:
                    switch (statorFront) {
                        case Base6Directions.Direction.Left:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Right:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Down:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Up:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                    }
                    break;
                case Base6Directions.Direction.Forward:
                    switch (statorFront) {
                        case Base6Directions.Direction.Left:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Right:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Down:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Up:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                    }
                    break;
                case Base6Directions.Direction.Left:
                    switch (statorFront) {
                        case Base6Directions.Direction.Left:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Right:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Down:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Up:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                    }
                    break;
                case Base6Directions.Direction.Right:
                    switch (statorFront) {
                        case Base6Directions.Direction.Left:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Right:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Down:
                            mfStatorOffset += MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Up:
                            mfStatorOffset += -MathHelper.PiOver2;
                            break;
                    }
                    break;
            }
        }
        public RotoFinger(IMyMotorAdvancedStator aStator, Logger aLogger, GTS aGTS, RotoFinger aParent = null) {
            okay = false;
            stator = new Stator(aStator, aLogger);
            //stator.reverse = true;
            g = aLogger;
            gts = aGTS;
            parent = aParent;
            if (aParent == null) {
                index = 0;
            }
            Identity = index++;
            
            try {
                //var dir = Base6Directions.GetIntVector(aStator.Top.Orientation.Up);
                IMyMotorAdvancedStator hingeRotor = null;
                gts.getByGrid(aStator.TopGrid.EntityId, ref hingeRotor);
                if (hingeRotor != null) {
                    hinge = new Stator(hingeRotor, aLogger);
                    okay = true;

                    var hingeUp = hingeRotor.Orientation.Up;
                    hingeRotor.CustomName = "Finger - " + Identity.ToString("D2") + " - Hinge - up:" + hingeUp;

                    // offset stator based on hinge up direction
                    switch (hingeUp) {
                        case Base6Directions.Direction.Backward:
                            mfHingeOffset = MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Forward:
                            mfHingeOffset = -MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Right:
                            mfHingeOffset = MathHelper.Pi;
                            break;
                        case Base6Directions.Direction.Left:
                            //stator.mfOffset = MathHelper.Pi;
                            break;
                    }
                    stator.mfOffset += mfHingeOffset;

                    // stator the front of the stator really depends on its up direction
                    // because after the first finger, the grid the stator is on will always have up=up
                    var statorFront = aStator.Orientation.Forward;
                    var statorUp = aStator.Orientation.Up;

                    aStator.CustomName = "Finger - " + Identity.ToString("D2") + " - Base - front:" + statorFront + " - up:" + statorUp;
                    
                    if (parent != null) {
                        stator.mfOffset -= parent.stator.mfOffset;
                        genStatorOffset(stator, statorFront);
                    }
                    
                    var hingeFront = hingeRotor.Orientation.Forward;
                    hingeRotor.CustomName += " - front:" + hingeFront;

                    // offset hinge based on forward dir
                    switch (hingeFront) {
                        case Base6Directions.Direction.Down:
                            //hinge.mfOffset = MathHelper.PiOver2;
                            break;
                        case Base6Directions.Direction.Left:
                            hinge.mfOffset = -MathHelper.PiOver2;
                            break;
                    }
                }
            } catch(Exception ex) {
                g.persist("Finger initialization FAILED: " + ex.ToString());
            }
        }
        bool hingeClose, statorClose;
        public bool close => hingeClose && statorClose;
        public void SetTargetZero() {
            stator.SetTarget(0);
            hinge.SetTarget(0);
        }
        public RotoFinger next() {
            RotoFinger result = null;
            if (okay) {
                var topGrid = hinge.TopGrid;
                if (topGrid != null) {
                    gts.getByGrid(topGrid.EntityId, ref piston);
                    IMyMotorAdvancedStator rotor = null;

                    Base6Directions.Direction dir = Base6Directions.Direction.Right;
                    if (piston == null) {
                        gts.getByGrid(hinge.TopGrid.EntityId, ref rotor);
                        if (rotor != null) {
                            dir = rotor.Orientation.Up;
                        }
                    } else {
                        dir = piston.Orientation.Up;
                        
                        gts.getByGrid(piston.TopGrid.EntityId, ref rotor);
                        
                    }

                    if (rotor != null) {
                        
                        result = new RotoFinger(rotor, g, gts, this);
                        
                        if (piston != null) {
                            piston.CustomName = "Finger - " + result.Identity.ToString("D2") + " - Piston - " + dir;
                        }
                        switch (dir) {
                            case Base6Directions.Direction.Right:
                                hinge.mfOffset += MathHelper.PiOver2;
                                break;
                        }

                        var bf = Base6Directions.GetVector(rotor.Orientation.Forward);

                        //var ab = MAF.angleBetween(tf, bf);
                        /*if (ab < 2 && ab > 1) {
                            var tu = Base6Directions.GetVector(top.Orientation.Up);
                            var br = -Base6Directions.GetVector(rotor.Orientation.Left);
                            var dot = tu.Dot(bf);
                            //g.persist("dot " + dot);
                            if (dot < 1) {
                                ab = -ab;
                            }
                        }*/
                        //g.persist(Identity + " ab " + ab);
                        //result.stator.mfOffset += (float)(ab + Math.PI);
                    }
                }
            }
            return result;
        }
        public void Update() {
            stator.Update();
            hinge.Update();
        }
        public void SetTarget(float aTarget) {
            mfTarget = aTarget;
            mode = FingerMode.hold;
            stator.SetTarget(aTarget);
            hinge.SetTarget(aTarget);
        }
  
        public void SetTarget(Vector3D aTarget) {
            mvTarget = aTarget;
            mode = FingerMode.point;
            stator.SetTarget(aTarget);
            hinge.SetTarget(aTarget);   
            
            
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
