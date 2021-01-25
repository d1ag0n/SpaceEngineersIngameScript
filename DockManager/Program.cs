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
        readonly List<Dock> mDocks;
        readonly IMyTextPanel lcd;
        States state = States.uninitialized;

        int index = 0;

        public enum States
        {
            uninitialized,
            initialized,
            calibrated,
            ready
        }

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            gts = new GTS(this, g);
            gts.getByTag("dockdisplay", ref lcd);

            mDocks = new List<Dock>();
        }
        void update() {
            g.log("update start");
            dockInfo();
            g.log("update complete");
        }

        void dockInfo() {
            if (mDocks.Count > 0) {
                var list = new Connector[mDocks.Count];
                int i = 0;
                for (; i < mDocks.Count; i++) {
                    list[i] = new Connector(Me, mDocks[i]);
                }
                IGC.SendBroadcastMessage("docks", Connector.ToCollection(list));
                g.log("dock info broadcasted ", i);
            } else {
                g.log("No docks");
            }
        }
        
        public void Save() {
            
        }
        
        void findDocks() {
            var list = new List<IMyPistonBase>();
            gts.getByTag("dock", list);

            for (int i = 0; i < list.Count; i++) {
                var d = new Dock(gts, g, list[i]);
                mDocks.Add(d);
            }
            
        }

        bool initDocks() {
            bool result = true;
            for (int i = 0; i < mDocks.Count; i++) {
                var r = mDocks[i].init();
                if (!r) {
                    result = false;
                }
            }
            return result;
        }

        double angleBetween(Vector3D a, Vector3D b) {
            var result = Math.Acos(a.Dot(b));
            //log("angleBetween ", result);
            return result;
        }
        void Main(string argument, UpdateType aUpdate) {
            if (aUpdate.HasFlag(UpdateType.Update10)) {
                try {
                    g.log("state ", state);
                    g.log("docks found ", mDocks.Count);
                    switch (state) {
                        case States.uninitialized:
                            findDocks();
                            state = States.initialized;
                            break;
                        case States.initialized:
                            if (initDocks()) {
                                state = States.calibrated;
                            }
                            break;
                        case States.calibrated:
                            var retracted = true;
                            for (int i = 0; i < mDocks.Count; i++) {
                                if (!mDocks[i].retract()) {
                                    retracted = false;
                                }
                                
                            }
                            if (retracted) {
                                state = States.ready;
                            }
                            break;
                        case States.ready:
                            update();
                            break;
                    }
                    
                } catch (Exception ex) {
                    g.log(ex);
                }
                
                if (null != lcd) {
                    argument = g.clear();
                    lcd.WriteText(argument);
                } else {
                    g.log("LCD write skipped.");
                    argument = g.clear();
                }
                Echo(argument);
                //Me.CustomData = argument;
                //Me.Enabled = false;
            }

        }
        Vector3D local2pos(Vector3D local, MatrixD world) =>
   Vector3D.Transform(local, world);
        Vector3D local2dir(Vector3D local, MatrixD world) =>
            Vector3D.TransformNormal(local, world);
        Vector3D world2pos(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world - local.Translation, MatrixD.Transpose(local));
        Vector3D world2dir(Vector3D world, MatrixD local) =>
            Vector3D.TransformNormal(world, MatrixD.Transpose(local));
    }
}
