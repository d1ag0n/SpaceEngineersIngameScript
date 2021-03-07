using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript {
    class RotoArm {
        bool okay;
        bool ready;
        readonly List<RotoFinger> fingers = new List<RotoFinger>();
        readonly RotoFinger firstFinger;
        RotoFinger lastFinger;
        readonly Logger g;
        readonly IMyTerminalBlock tip;
        readonly V3DLag tipLag = new V3DLag(18);
        Vector3D mvTarget;
        double mdTargetDistance;

        enum ArmState {
            preinit,
            init,
            ready
        }
        ArmState state = ArmState.preinit;

        void setTarget(Vector3D aWorld) {
            mvTarget = MAF.world2pos(tip.WorldMatrix.Translation, firstFinger.stator.WorldMatrix);
            mdTargetDistance = mvTarget.Length();
        }
        public RotoArm(IMyMotorAdvancedStator aStator, GTS aGTS, Logger aLogger) {
            g = aLogger;
            fingers.Add(firstFinger = lastFinger = new RotoFinger(aStator, aLogger, aGTS));
            okay = lastFinger.okay;
            if (okay) {
                aGTS.getByTag("armtip", ref tip);
                if (tip == null) {
                    mvTarget = Vector3D.Zero;
                }
            }
        }
        public double ModifyTarget(Vector3 aMove) {
            if (aMove != Vector3.Zero) {
                aMove.Z = -aMove.Z;
                mvTarget += aMove;
                mdTargetDistance = mvTarget.Length();
            }
            return mdTargetDistance;
        }
        // last finger is free
        // odd number of fingers should have a separate calculation method
        // even + 1 number of fingers is easier to start with

        void crane() {
            // always keep last finger free

            // pi radians distributed over n bends

            var count = fingers.Count - 1;

            // base finger angle 45

            var d = (count * (count + 1)) / 2;

            // total bend = pi
            // remaining bend = total bend - bend of first finger

            var ffa = firstFinger.hinge.Angle;
        }

        bool updateFingers() {
            var result = true;
            foreach (var f in fingers) {
                if (!f.Update()) {
                    result = false;
                }
            }
            return result;
        }
        bool updateFingers(float aTurn, float aBend) {
            var result = true;
            var one = true;
            float total = 0;
            foreach (var f in fingers) {
                if (one) {
                    total += elevationAngle;
                    f.SetTargetTurnBend(aTurn, aBend + elevationAngle);
                    one = false;
                } else if (f == fingers[fingers.Count - 1]) {
                    f.SetTargetTurnBend(aTurn, MathHelper.Pi - total);
                } else {
                    f.SetTargetTurnBend(aTurn, aBend);
                }
                
                total += aBend;

                if (!f.Update()) {
                    result = false;
                }
            }
            return result;
        }
        void doUpdate(Vector3D aTipWorld) {
            bool result;
            switch (state) {
                case ArmState.preinit:
                    angleDistance = MathHelper.Pi / fingers.Count;
                    foreach (var f in fingers) {
                        f.SetTargetTurnBend(0, angleDistance);
                    }
                    state = ArmState.init;
                    break;
                case ArmState.init:
                    if (updateFingers()) {
                        setTarget(aTipWorld);
                        state = ArmState.ready;
                    }
                    break;
                case ArmState.ready:
                    translateTip(aTipWorld);
                    updateFingers(anglePoint, angleDistance);
                    break;
            }
        }

        public bool Update() {
            bool result = false;
            if (okay) {
                if (ready) {
                    result = true;
                    doUpdate(tipLag.update(tip.WorldMatrix.Translation));
                } else {
                    var f = lastFinger.nextFinger;
                    if (f == null) {
                        
                        ready = true;
                    } else {
                        okay = f.okay;
                        fingers.Add(lastFinger = f);
                    }
                }
            }
            return result;
        }
        float angleDistance;
        float anglePoint = 0;
        float elevationAngle = 0;
        const float angleStep = 0.001f;
        void translateTip(Vector3D aTipWorld) {
            if (mvTarget == Vector3D.Zero) {
                return;
            }
            var local = MAF.world2pos(aTipWorld, firstFinger.stator.stator.WorldMatrix);
            var dist = local.Length();
            var dif = dist - mdTargetDistance;

            if (Math.Abs(dif) > 1) {
                if (dif > 0) {
                    angleDistance *= 1.01f;
                } else {
                    //angleDistance -= angleStep;
                    angleDistance *= 0.999f;
                }
                if (angleDistance < 0) {
                    angleDistance = 0;
                } else {
                    //angleDistance *= 0.9f;
                }
                
            }
            g.log("angle distance ", angleDistance);
            var a = fingers[1].hinge.Angle;
            var o = fingers[1].hinge.mfOffset;
            float w = a - o;
            MathHelper.LimitRadians(ref w);
            g.log("hinge 1 angle   ", a); 
            g.log("hinge 1 offset  ", o);
            g.log("wrap            ", w);
            g.log("elevation angle ", elevationAngle.ToString());
            float elevationDif = (float)(local.Y - mvTarget.Y);
            g.log("elevation dif   ", elevationDif);
            //g.log(mvTarget);
            // g.log(local);

            if (Math.Abs(elevationDif) < 1.0) {

            } else {
                const float elEpsilon = 0.001f;
                const float elAdd = 1.1f;
                const float elSub = 0.99f;
                if (elevationDif < 0) {
                    // too low
                    if (Math.Abs(elevationAngle) < elEpsilon) {
                        elevationAngle = -elEpsilon;
                    }
                    if (elevationAngle > 0) {
                        elevationAngle *= elSub;
                    } else {
                        elevationAngle *= elAdd;
                    }
                } else {
                    // too high
                    if (Math.Abs(elevationAngle) < elEpsilon) {
                        elevationAngle = elEpsilon;
                    }
                    if (elevationAngle < 0) {

                    }
                    elevationAngle *= elAdd;
                }
            }
        }
    }
}
