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
        public readonly IMyMotorAdvancedStator rotor;
        public readonly IMyMotorAdvancedStator hinge;
        public IMyEntity tip => hinge.Top;
        readonly Logger g;
        readonly float hingeOffset;

        public bool okay {
            get; private set;
        }
        public Finger(IMyMotorAdvancedStator aRotor, Logger aLogger, bool first = false) {
            okay = false;
            rotor = aRotor;
            g = aLogger;
            try {
                var dir = Base6Directions.GetIntVector(aRotor.Top.Orientation.Up);
                var attachment = aRotor.TopGrid.GetCubeBlock(aRotor.Top.Position + dir);
                if (attachment != null) {
                    var block = attachment.FatBlock;
                    if (block is IMyMotorAdvancedStator && block.BlockDefinition.SubtypeId == "LargeHinge") {
                        hinge = block as IMyMotorAdvancedStator;
                        okay = true;
                    }
                }
                if (!okay) {
                    g.persist("Finger initialization FAILED");
                } else {
                    if (false) {

                        var m = hinge.WorldMatrix;
                        var t = m.Translation;

                        g.persist(g.gps("H F", t + m.Forward));
                        g.persist(g.gps("H B", t + m.Backward));
                        g.persist(g.gps("H L", t + m.Left));
                        g.persist(g.gps("H R", t + m.Right));
                        g.persist(g.gps("H U", t + m.Up));
                        g.persist(g.gps("H D", t + m.Down));

                        m = rotor.Top.WorldMatrix;
                        t = m.Translation;

                        g.persist(g.gps("R F", t + m.Forward));
                        g.persist(g.gps("R B", t + m.Backward));
                        g.persist(g.gps("R L", t + m.Left));
                        g.persist(g.gps("R R", t + m.Right));
                        g.persist(g.gps("R U", t + m.Up));
                        g.persist(g.gps("R D", t + m.Down));


                    }
                }
            } catch(Exception ex) {

                g.persist("Finger initialization FAILED: " + ex.ToString());
            }
        }

        public Finger next() {
            Finger result = null;
            var top = hinge.Top;
            if (okay && top.BlockDefinition.SubtypeId == "LargeHingeHead") {
                var dir = Base6Directions.GetIntVector(top.Orientation.Left);
                var attachment = hinge.TopGrid.GetCubeBlock(top.Position + dir);
                if (attachment != null) {
                    var block = attachment.FatBlock;
                    if (block.BlockDefinition.SubtypeId == "LargeAdvancedStator") {
                        result = new Finger(block as IMyMotorAdvancedStator, g);
                    }
                }
            }
            return result;
        }

        public void zero() {
            setRotorAngle(0);
            setHingeAngle(0);
        }
        public void setHingeAngle(float aAngle) {
            float angle = aAngle - hinge.Angle;
            hinge.TargetVelocityRad = angle * 0.1f;
        }
        public void setRotorAngle(float aAngle) {
            // aAngle = 0
            // angle = 6.28
            // 6.28 = 0 - angle
            
            var angle = aAngle - rotor.Angle;
            if (angle > MathHelper.Pi) {
                angle -= MathHelper.TwoPi;
            } else if (angle < -MathHelper.Pi) {
                angle += MathHelper.TwoPi;
            }
            rotor.TargetVelocityRad = angle;

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
        }

        public void info() {
            g.log("Finger");
            g.log("Rotor angle ", rotor.Angle);
            g.log("Hinge angle ", hinge.Angle);
        }
    }
}
