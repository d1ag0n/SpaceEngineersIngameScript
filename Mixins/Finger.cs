using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    class Finger
    {
        Vector3D mvTarget;
        float mfTarget;
        FingerMode mode;
        // orange #EE4C00
        // blue #085FA6
        // red #B20909
        // yellow #E8B323   
        public readonly Stator stator;
        public readonly Stator hinge;
        public IMyEntity tip => hinge == null ? null : hinge.Top;
        readonly Logger g;
        public readonly Finger parent;
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
            stator.Stop();
            hinge.Stop();
            stopped = true;
        }
        public void Go() {
            stator.Go();
            hinge.Go();
            stopped = false;
        }
        public Finger(IMyMotorAdvancedStator aStator, Logger aLogger, Finger aParent = null) {
            okay = false;
            stator = new Stator(aStator, aLogger);
            
            g = aLogger;
            parent = aParent;
            if (aParent == null) {
                index = 0;
            }
            Identity = index++;
            aStator.CustomName = "Finger - Stator - " + Identity.ToString("D3");
            try {
                var dir = Base6Directions.GetIntVector(aStator.Top.Orientation.Up);
                var attachment = aStator.TopGrid.GetCubeBlock(aStator.Top.Position + dir);
                Base6Directions.Direction hingeDir;
                if (attachment != null) {
                    var block = attachment.FatBlock as IMyTerminalBlock;
                    if (block is IMyMotorAdvancedStator && block.BlockDefinition.SubtypeId == "LargeHinge") {
                        hinge = new Stator(block as IMyMotorAdvancedStator, aLogger);
                        block.CustomName = "Finger - Hinge  - " + Identity.ToString("D3") + " " + block.Orientation.Forward;
                        okay = true;
                        switch (block.Orientation.Forward) {
                            case Base6Directions.Direction.Right:
                                stator.mfOffset = -MathHelper.PiOver2;
                                break;
                            case Base6Directions.Direction.Left:
                                stator.mfOffset = MathHelper.PiOver2;
                                break;
                        }
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
        public Finger next() {
            Finger result = null;
            if (okay) {
                var top = hinge.Top;
                if (top != null && top.BlockDefinition.SubtypeId == "LargeHingeHead") {
                    var dir = Base6Directions.GetIntVector(top.Orientation.Left);
                    var attachment = hinge.TopGrid.GetCubeBlock(top.Position + dir);
                    if (attachment != null) {
                        var block = attachment.FatBlock;
                        if (block.BlockDefinition.SubtypeId == "LargeAdvancedStator") {
                            result = new Finger(block as IMyMotorAdvancedStator, g, this);
                            
                            var tf = Base6Directions.GetVector(top.Orientation.Forward);
                            var bf = Base6Directions.GetVector(block.Orientation.Forward);
                            var ab = MAF.angleBetween(tf, bf);
                            if (ab < 2 && ab > 1) {
                                var tu = Base6Directions.GetVector(top.Orientation.Up);
                                var br = -Base6Directions.GetVector(block.Orientation.Left);
                                var dot = tu.Dot(bf);
                                //g.persist("dot " + dot);
                                if (dot < 1) {
                                    ab = -ab;
                                }
                            }
                            //g.persist(Identity + " ab " + ab);
                            result.stator.mfOffset += (float)(ab + Math.PI);
                        }
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
            //stator.Info();
            g.log("Hinge");
            hinge.Info();
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
