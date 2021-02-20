using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        readonly GTS gts;
        readonly Logger g;
        readonly IMyTextPanel lcd;

        readonly Finger firstFinger;
        Finger lastFinger;
        readonly List<IMyMotorAdvancedStator> controlRotors = new List<IMyMotorAdvancedStator>();
        readonly List<Finger> fingers = new List<Finger>();
        bool walkComplete = false;
        readonly IMyCockpit control;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);
            gts.getByTag("control", ref lcd);
            if (lcd != null) {
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;
            }

            gts.initListByTag("control", controlRotors);
            g.persist("control rotors found " + controlRotors.Count);

            IMyMotorAdvancedStator first = null;
            gts.getByTag("arm", ref first);
            var finger = new Finger(first, g, true);
            if (finger.okay) {
                fingers.Add(finger);
                firstFinger = finger;
                //g.persist(g.gps("firstFinger", firstFinger.hinge.WorldMatrix.Translation));
            } else {
                walkComplete = true;
                g.persist("firstFinger not okay");
            }
            gts.getByTag("control", ref control);
        }

        Vector3D target = Vector3D.Zero;
        void doWalk() {
            Finger finger = null;
            if (!walkComplete) {
                if (lastFinger == null) {
                    finger = firstFinger.next();
                } else {
                    finger = lastFinger.next();
                }
                if (finger == null) {
                    walkComplete = true;
                } else {
                    if (finger.okay) {
                        fingers.Add(finger);
                        lastFinger = finger;
                    }
                }
            } else {
                g.log("walk complete found ", fingers.Count, " fingers");
                if (firstFinger != null) {
                    firstFinger.info();
                }
            }
        }
        float angle = 0;
        void procArgument(string arg) {
            float val;
            if (arg == "pi") {
                angle = MathHelper.Pi;
            } else if (arg == "2pi") {
                angle = MathHelper.TwoPi;
            } else if (float.TryParse(arg, out val)) {
                MathHelper.LimitRadians(ref val);
                angle = val;
            }
            g.log("target angle ", angle);
        }
        V3DLag targetLag = new V3DLag(6);
        public void Main(string argument, UpdateType updateSource) {
            try {
                var bcontrol = control != null;
                procArgument(argument);
                doWalk();
                if (controlRotors.Count > 0 && control != null) {
                    g.log("control okay");
                    var target = Vector3D.Zero;
                    IMyEntity tip = null;
                    if (lastFinger == null) {
                        if (firstFinger.okay) {
                            tip = firstFinger.tip;
                            if (tip == null) {
                                tip = firstFinger.hinge;
                            }
                        }
                    } else {
                        if (lastFinger.okay) {
                            tip = lastFinger.tip;
                            if (tip == null) {
                                tip = lastFinger.hinge;
                            }
                        }
                    }
                    if (tip != null) {
                        target = tip.WorldMatrix.Translation;
                    }
                    
                    foreach (var r in controlRotors) {
                        if (target == Vector3D.Zero) {
                            r.TargetVelocityRad = 0;
                        } else {
                            var dif = control.WorldMatrix.Translation - r.WorldMatrix.Translation;
                            pointRotoAtTarget(r, targetLag.update(target - dif));
                        }
                        //pointRotoAtTarget(r, finger.WorldMatrix.Translation);
                    }
                }
                foreach (var f in fingers) {
                    //f.zero();
                    f.setHingeAngle(0.5f);
                    f.setRotorAngle(angle);
                }
            } catch (Exception ex) {
                g.persist(ex.ToString());
            }
            var str = g.clear();
            if (lcd != null) {
                lcd.WriteText(str);
            }
            Echo(str);
        }
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
