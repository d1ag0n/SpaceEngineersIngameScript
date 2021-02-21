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
        // orange #EE4C00
        // blue #085FA6
        // red #B20909
        // yellow #E8B323   
        public readonly IMyMotorAdvancedStator stator;
        public readonly IMyMotorAdvancedStator hinge;
        public IMyEntity tip => hinge == null ? null : hinge.Top;
        readonly Logger g;
        public readonly Finger parent;
        readonly float hingeOffset = MathHelper.Pi;
        float statorOffset;
        //float parentHingeOffset = 0;
        //float parentStatorOffset = 0;

        //float hingeValue, statorValue;
        //bool hingeLocked, statorLocked;
        static int index = 0;

        //const float torque = 1000000000f;
        //const float hingeLimit = 1.570796f;
        const float epsilon = 0.2f * (MathHelper.Pi / 180.0f);

        public bool okay {
            get; private set;
        }

        public Finger(IMyMotorAdvancedStator aStator, Logger aLogger, Finger aParent = null) {
            okay = false;
            stator = aStator;
            g = aLogger;
            parent = aParent;
            try {
                var dir = Base6Directions.GetIntVector(stator.Top.Orientation.Up);
                var attachment = stator.TopGrid.GetCubeBlock(stator.Top.Position + dir);
                if (attachment != null) {
                    var block = attachment.FatBlock;
                    if (block is IMyMotorAdvancedStator && block.BlockDefinition.SubtypeId == "LargeHinge") {
                        hinge = block as IMyMotorAdvancedStator;
                        okay = true;
                    }
                }
                
                if (okay) {
                    index++;

                    stator.CustomName = "Finger - Stator - " + index.ToString("D3");
                    stator.TargetVelocityRad = 0;
                    //stator.Torque = torque;
                    //stator.BrakingTorque = torque;
                    stator.RotorLock = false;
                    //stator.LowerLimitRad = float.MinValue;
                    //stator.UpperLimitRad = float.MaxValue;
                    if (parent != null) {

                        /*
                        switch (stator.Orientation.Forward) {
                            case Base6Directions.Direction.Forward:
                                statorOffset = MathHelper.Pi;
                                break;
                            case Base6Directions.Direction.Backward:
                                break;
                            case Base6Directions.Direction.Up:
                                statorOffset = MathHelper.PiOver2;
                                break;
                        }*/
                        //g.persist(index + " rotor forward " + stator.Orientation.Forward);
                    }

                    hinge.CustomName = "Finger - Hinge  - " + index.ToString("D3");
                    hinge.TargetVelocityRad = 0;
                    //hinge.Torque = torque;
                    //hinge.BrakingTorque = torque;
                    hinge.RotorLock = false;

                    switch (hinge.Orientation.Forward) {
                        case Base6Directions.Direction.Left: hingeOffset = -MathHelper.PiOver2; break;
                        case Base6Directions.Direction.Right: hingeOffset = MathHelper.PiOver2; break;
                        case Base6Directions.Direction.Backward: hingeOffset = 0; break;
                    }


                    //g.persist("finger " + index + " hingeOffset " + hingeOffset.ToString());

                    //g.persist("hinge front " + hinge.Orientation.Forward);
                    //g.persist("rotor front " + stator.Top.Orientation.Forward);
                }
                ///*
                if (aParent == null) {
                    return;
                    var m = stator.WorldMatrix;
                    var t = m.Translation;

                    g.persist(g.gps("S F", t + m.Forward));
                    g.persist(g.gps("S B", t + m.Backward));
                    g.persist(g.gps("S L", t + m.Left));
                    g.persist(g.gps("S R", t + m.Right));
                    g.persist(g.gps("S U", t + m.Up));
                    g.persist(g.gps("S D", t + m.Down));

                    m = hinge.Top.WorldMatrix;
                    t = m.Translation;

                    g.persist(g.gps("T F", t + m.Forward));
                    g.persist(g.gps("T B", t + m.Backward));
                    g.persist(g.gps("T L", t + m.Left));
                    g.persist(g.gps("T R", t + m.Right));
                    g.persist(g.gps("T U", t + m.Up));
                    g.persist(g.gps("T D", t + m.Down));
                    return;
                    m = stator.Top.WorldMatrix;
                    t = m.Translation;

                    g.persist(g.gps("R F", t + m.Forward));
                    g.persist(g.gps("R B", t + m.Backward));
                    g.persist(g.gps("R L", t + m.Left));
                    g.persist(g.gps("R R", t + m.Right));
                    g.persist(g.gps("R U", t + m.Up));
                    g.persist(g.gps("R D", t + m.Down));

                    m = hinge.WorldMatrix;
                    t = m.Translation;

                    g.persist(g.gps("H F", t + m.Forward));
                    g.persist(g.gps("H B", t + m.Backward));
                    g.persist(g.gps("H L", t + m.Left));
                    g.persist(g.gps("H R", t + m.Right));
                    g.persist(g.gps("H U", t + m.Up));
                    g.persist(g.gps("H D", t + m.Down));

                    
                }//*/
                
            } catch(Exception ex) {

                g.persist("Finger initialization FAILED: " + ex.ToString());
            }
        }

        public Finger next() {
            Finger result = null;
            var top = hinge.Top;
            if (okay && top != null && top.BlockDefinition.SubtypeId == "LargeHingeHead") {
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
                        //g.persist(index + " ab " + ab);
                        
                        result.statorOffset = (float)(ab + Math.PI);
                    }
                }
            }
            return result;
        }

        public void zero() {
            setStatorAngle(0);
            setHingeAngle(0);
        }

        public void setHingeAngle(float aAngle) =>
            setStator(hinge, (aAngle - hinge.Angle), 0.1f);
        public void pointAtTarget(Vector3D aTarget) {
            var local = MAF.world2pos(aTarget, stator.WorldMatrix);
            var localFlat = new Vector3D(local.X, 0, local.Z);
            double dot;
            var ab = MAF.angleBetween(Vector3D.Forward, localFlat, out dot);

            if (hinge.Angle > 0) {
                ab += Math.PI;
            }
            //ab = MathHelper.Pi - ab;
            /*
            MathHelper.LimitRadians(ref angle);
            if (angle > MathHelper.Pi) {
                angle -= MathHelper.TwoPi;
            } else if (angle < -MathHelper.Pi) {
                angle += MathHelper.TwoPi;
            }
            setStator(stator, angle, 0.1f);
            //*/
            g.log("stator point at target ab " + ab + " dot " + dot);
            setStatorAngle((float)ab, false);

            ab = MAF.angleBetween(Vector3D.Up, local, out dot);
            g.log("hinge point at target ab " + ab + " dot " + dot);

            setHingeAngle(-(float)ab);
        }
        public void setStatorAngle(float aAngle, bool offset = true) {
            if (offset) {
                //var phOffset = parent == null ? 0f : parent.hingeOffset;
                //var psOffset = parent == null ? 0f : parent.statorOffset;
                aAngle = (aAngle + hingeOffset - statorOffset) - stator.Angle;
            } else {
                aAngle = (aAngle + hingeOffset) - stator.Angle;
            }
            MathHelper.LimitRadians(ref aAngle);
            if (aAngle > MathHelper.Pi) {
                aAngle -= MathHelper.TwoPi;
            } else if (aAngle < -MathHelper.Pi) {
                aAngle += MathHelper.TwoPi;
            }
            // = !
            setStator(stator, aAngle, 0.1f);
        }
        
        void setStator(IMyMotorAdvancedStator aStator, float angle, float factor) {
            if (Math.Abs(angle) < epsilon) {
                aStator.TargetVelocityRad = 0;
            } else {
                aStator.TargetVelocityRad = angle * factor;
            }
        }
        public void info() {
            g.log("Finger");
            g.log("Rotor angle ", stator.Angle);
            g.log("Hinge angle ", hinge.Angle);
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
        }
    }
}
