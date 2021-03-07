using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    /// <summary>
    /// For helping to control an arm made of rotors. The arm begins at first rotor.
    /// It then expects an additional rotor with it's up perpendicular to the up of the first rotor
    /// Then an optional piston. Blocks out of this order are undefined
    /// You can add an arbitrary number of fingers to the arm.
    /// Currently the class will handle setting the angle of each finger.
    /// Pointing by target is currently broken
    /// </summary>
    class RotoFinger
    {
        Vector3D mvTarget;
        float mfTurnTarget;
        float mfBendTarget;
        FingerMode mode;
        // orange #EE4C00
        // blue #085FA6
        // red #B20909
        // yellow #E8B323   
        /// <summary>
        /// bottom of finger handles yaw/turn
        /// </summary>
        public readonly Stator stator;
        /// <summary>
        /// top of finger handles pitch/bend
        /// </summary>
        public readonly Stator hinge;

        
        IMyExtendedPistonBase piston;
        public IMyEntity tip => hinge == null ? null : hinge.Top;
        readonly Logger g;
        readonly GTS gts;

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
        
        
        public readonly int Identity = 0;

        const float epsilon = 1.0f * (MathHelper.Pi / 180.0f);


        enum FingerMode {
            hold,
            point,
            manual
        }

        public bool okay {
            get; private set;
        }
        public bool stopped { get; private set; }
        
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
            if (Identity == 4)
            g.persist(aStator.CustomName);
            try {
                IMyMotorAdvancedStator hingeRotor = null;
                gts.getByGrid(aStator.TopGrid.EntityId, ref hingeRotor);
                if (hingeRotor != null) {
                    hinge = new Stator(hingeRotor, aLogger);
                    okay = true;
                    if (hinge.TopGrid != null) {
                        gts.getByGrid(hinge.TopGrid.EntityId, ref piston);
                    }

                    float ab;
                    var dot = double.NaN;
                    var hingeUpDir = hinge.stator.Orientation.Up;
                    var statorTopLeft = Base6Directions.GetVector(stator.Top.Orientation.Left);
                    var statorTopBack = -Base6Directions.GetVector(stator.Top.Orientation.Forward);
                    var hingeUp = Base6Directions.GetVector(hingeUpDir);
                    ab = (float)MAF.angleBetween(hingeUp, statorTopLeft);
                    if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                        dot = hingeUp.Dot(statorTopBack);
                        if (dot < 0) {
                            ab = -ab;
                        }
                    }
                    stator.mfOffset += ab;
                    //if (Identity == 4) g.persist($"ab hu stl {ab} {dot}");
                    if (previousFinger != null) {
                        var previousHingeTopUp = Base6Directions.GetVector(previousFinger.hinge.stator.Top.Orientation.Up);
                        var statorFront = Base6Directions.GetVector(stator.stator.Orientation.Forward);
                        var statorLeft = Base6Directions.GetVector(stator.stator.Orientation.Left);
                        var statorRight = -Base6Directions.GetVector(stator.stator.Orientation.Left);
                        if (previousFinger.piston == null) {
                            ab = (float)MAF.angleBetween(previousHingeTopUp, statorLeft);
                            if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                                dot = previousHingeTopUp.Dot(statorFront);
                                if (dot < 0) {
                                    ab = -ab;
                                }
                            }
                            stator.mfOffset += ab;
                            //if (Identity == 4) g.persist($"ab phtu sl {ab} {dot}");
                        } else {
                            var previousPistonLeft = Base6Directions.GetVector(previousFinger.piston.Orientation.Left);
                            ab = (float)MAF.angleBetween(previousHingeTopUp, previousPistonLeft);
                            if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                                var previousPistonFront = Base6Directions.GetVector(previousFinger.piston.Orientation.Forward);
                                dot = previousHingeTopUp.Dot(previousPistonFront);
                                if (dot < 0) {
                                    ab = -ab;
                                }
                            }
                            stator.mfOffset += ab;
                            //if (Identity == 4) g.persist($"ab phtu ppl {ab} {dot}");

                            var previousPistonTopLeft = Base6Directions.GetVector(previousFinger.piston.Top.Orientation.Left);
                            ab = (float)MAF.angleBetween(previousPistonTopLeft, statorLeft);
                            if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                                dot = previousPistonTopLeft.Dot(statorFront);
                                if (dot < 0) {
                                    ab = -ab;
                                }
                            }
                            stator.mfOffset += ab;
                            //if (Identity == 4) g.persist($"ab pptl sl {ab} {dot}");
                        }

                    }

                    hingeRotor.CustomName = "Finger - " + Identity.ToString("D2") + " - Bend";

                    if (aStator.Top != null) {
                        var statorTopUp = Base6Directions.GetVector(aStator.Top.Orientation.Up);
                        var hingeBack = -Base6Directions.GetVector(hinge.stator.Orientation.Forward);
                        var hingeLeft = Base6Directions.GetVector(hinge.stator.Orientation.Left);

                        ab = (float)MAF.angleBetween(statorTopUp, hingeBack);
                        dot = double.NaN;

                        if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                            dot = statorTopUp.Dot(hingeLeft);
                            if (dot < 0) {
                                ab = -ab;
                            }
                        }
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

                    if (rotor != null && rotor.Top != null) {
                        result = new RotoFinger(rotor, g, gts, this);
                        
                        var hingeTopRight = -Base6Directions.GetVector(hinge.Top.Orientation.Left);
                        var hingeTopBack = -Base6Directions.GetVector(hinge.Top.Orientation.Forward);
                        IMyTerminalBlock block = piston;
                        if (block == null) {
                            block = result.stator.stator;
                        }
                        var nextUp = Base6Directions.GetVector(block.Orientation.Up);
                        double dot;
                        var ab = MAF.angleBetween(nextUp, hingeTopBack);
                        
                        if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                            dot = nextUp.Dot(hingeTopRight);
                            if (dot < 0) {
                                ab = -ab;
                            }
                        }
                        hinge.mfOffset += (float)ab;
                        
                        if (piston != null) {
                            piston.CustomName = "Finger - " + result.Identity.ToString("D2") + " - Reach";
                        }
                    }
                }
            }
            return result;
        }
        public bool Update() {
            if (mode == FingerMode.hold) {
                //g.log("update turn ", mfTurnTarget, " bend ", mfBendTarget);
            } else if (mode == FingerMode.point) {
                g.log("update pointing");
            } else if (mode == FingerMode.manual) {
                return true;
            }
            return stator.Update() & hinge.Update();
        }
        public void SetManual() {
            mode = FingerMode.manual;
            hinge.stator.UpperLimitRad = float.MaxValue;
            hinge.stator.LowerLimitRad = float.MinValue;
        }

        public void SetTargetTurnBend(float aTurn, float aBend) {
            
            mfTurnTarget = aTurn;
            mfBendTarget = aBend;
            mode = FingerMode.hold;

            stator.SetTarget(aTurn);
            stator.reverse = false;

            hinge.SetTarget(aBend);
            hinge.reverse = false;
        }
  
        public void SetTargetWorld(Vector3D aTarget) {
            
            mvTarget = aTarget;
            mode = FingerMode.point;
            
            stator.SetTarget(aTarget);
            stator.reverse = true;

            hinge.SetTarget(aTarget);
            hinge.reverse = false;
        }

        public void Info() {
            g.log("Finger " + Identity);
            g.log("Turn");
            stator.Info();
            g.log("Bend");
            hinge.Info();
        }
    }
}
