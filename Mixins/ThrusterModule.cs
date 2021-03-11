using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript {
    class ThrusterModule : Module<IMyThrust> {

        
        readonly List<Thrust> mThrust = new List<Thrust>();
        readonly List<IMyParachute> mParachutes = new List<IMyParachute>();
        
        public ThrusterModule() {
            Update = Organize;
        }
        public override bool Accept(IMyTerminalBlock aBlock) {
            if (aBlock is IMyParachute) {
                mParachutes.Add(aBlock as IMyParachute);
                return true;
            }
            
            if (aBlock is IMyThrust) {
                mThrust.Add(new Thrust(aBlock as IMyThrust));
                return true;
            }
            return false;
        }
        void Organize() {
            var ctr = ModuleManager.controller;
            if (ctr != null) {
                var rc = ctr.Remote;
                if (rc != null) {
                    foreach (var t in mThrust) {
                        Organize(rc, t);
                    }
                    Update = UpdateAction;
                }
            }
        }
        void Organize(IMyShipController aController, Thrust aThrust) {
            var o = aController.Orientation;
            var f = aThrust.Engine.Orientation.Forward;
            
            if (f == o.Forward) {
                aThrust.Direction = Base6Directions.Direction.Forward;
            } else if (f == o.Up) {
                aThrust.Direction = Base6Directions.Direction.Up;
            } else if (f == o.Left) {
                aThrust.Direction = Base6Directions.Direction.Left;
            } else if (f == Base6Directions.GetOppositeDirection(o.Forward)) {
                aThrust.Direction = Base6Directions.Direction.Backward;
            } else if (f == Base6Directions.GetOppositeDirection(o.Up)) {
                aThrust.Direction = Base6Directions.Direction.Down;
            } else if (f == Base6Directions.GetOppositeDirection(o.Left)) {
                aThrust.Direction = Base6Directions.Direction.Right;
            }

        }

        public override bool Remove(IMyTerminalBlock aBlock) {
            if (aBlock is IMyParachute) {
                var index = mParachutes.IndexOf(aBlock as IMyParachute);
                if (index > -1) {
                    mParachutes.RemoveAt(index);
                    return true;
                }
            } else if (aBlock is IMyThrust) {
                for (int i = mThrust.Count - 1; i >=0; i--) {
                    var t = mThrust[i];
                    if (t.Engine.EntityId == aBlock.EntityId) {
                        mThrust.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }
        void UpdateAction() {
            
            
            foreach (var t in Blocks) {
                //g.log(t.CustomName);
            }

        }
        public IMyThrust Get(int aIndex) {
            IMyThrust result = null;
            if (-1 < aIndex && Blocks.Count > aIndex) {
                result = Blocks[aIndex];
            }
            return result;
        }
    }
}
