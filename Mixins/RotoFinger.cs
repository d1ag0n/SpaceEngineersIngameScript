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
                    if (previousFinger != null) {
                        var previousHingeTopUp = Base6Directions.GetVector(previousFinger.hinge.stator.Top.Orientation.Up);
                        var statorFront = Base6Directions.GetVector(stator.stator.Orientation.Forward);
                        var statorLeft = Base6Directions.GetVector(stator.stator.Orientation.Left);
                        if (previousFinger.piston == null) {
                            ab = (float)MAF.angleBetween(previousHingeTopUp, statorLeft);
                            if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                                dot = previousHingeTopUp.Dot(statorFront);
                                if (dot < 0) {
                                    ab = -ab;
                                }
                            }
                            stator.mfOffset += ab;
                        } else {
                            var previousPistonLeft = Base6Directions.GetVector(previousFinger.piston.Orientation.Left);
                            var previousPistonTopLeft = Base6Directions.GetVector(previousFinger.piston.Top.Orientation.Left);
                            ab = (float)MAF.angleBetween(previousHingeTopUp, previousPistonLeft);
                            if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                                var previousPistonFront = Base6Directions.GetVector(previousFinger.piston.Orientation.Forward);
                                dot = previousHingeTopUp.Dot(previousPistonLeft);
                            }
                            stator.mfOffset += ab;
                            ab = (float)MAF.angleBetween(previousPistonTopLeft, statorLeft);
                            if (MAF.nearEqual(ab, MathHelper.PiOver2)) {
                                dot = previousPistonTopLeft.Dot(statorFront);
                                if (dot < 0) {
                                    ab = -ab;
                                }
                            }
                            stator.mfOffset += ab;
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

                    if (rotor != null) {
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
        public void Update() {
            stator.Update();
            hinge.Update();
        }

        public void SetTurnTarget(float aTarget) {
            mfTurnTarget = aTarget;
            mode = FingerMode.hold;
            stator.SetTarget(aTarget);
            stator.reverse = false;
        }
        public void SetBendTarget(float aTarget) {
            mfBendTarget = aTarget;
            mode = FingerMode.hold;
            hinge.SetTarget(aTarget);
            stator.reverse = false;
        }
  
        public void SetTarget(Vector3D aTarget) {
            mvTarget = aTarget;
            mode = FingerMode.point;
            stator.SetTarget(aTarget);
            hinge.SetTarget(aTarget);
            stator.reverse = true;
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
