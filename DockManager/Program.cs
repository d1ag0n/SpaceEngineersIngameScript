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
        readonly Dictionary<long, Dock> mDicDocks;
        readonly List<Dock> mListDocks;
        
        readonly IMyTextPanel lcd;
        States state = States.uninitialized;

        int index = 0;
        int count = 0;
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

            mDicDocks = new Dictionary<long, Dock>();
            mListDocks = new List<Dock>();
            IGC.UnicastListener.SetMessageCallback("dockcommand");
            
        }
        void update() {
            g.log("update start");
            count++;
            if (count > 6) {
                count = 0;
            }
            if (count == 0) {
                dockInfo();
            }
            for (int i = 0; i < mListDocks.Count; i++) {
                mListDocks[i].update();
            }
            
            g.log("update complete");
        }

        void dockInfo() {
            if (mListDocks.Count > 0) {

                var list = new List<Connector>();
                for (int i = 0; i < mListDocks.Count; i++) {
                    list.Add(new Connector(Me, mListDocks[i]));
                }
                IGC.SendBroadcastMessage("docks", Connector.ToCollection(list));
                g.log("dock info broadcasted ", list.Count);
            } else {
                g.log("No docks");
            }
        }
        
        public void Save() {
            
        }
        
        void findDocks() {
            var list = new List<IMyPistonBase>();
            gts.getByTag("dock", list);
            g.log("tag lookup found ", list.Count);
            for (int i = 0; i < list.Count; i++) {
                var d = new Dock(gts, g, list[i]);
                mDicDocks.Add(d.X.EntityId, d);
                mListDocks.Add(d);
            }
        }

        bool initDocks() {
            bool result = true;
            for (int i = 0; i < mListDocks.Count; i++) {
                var r = mListDocks[i].init();
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
        void processDockMessage(DockMessage aMessage) {
            Dock d;
            if (mDicDocks.TryGetValue(aMessage.DockId, out d)) {

                switch (aMessage.Command) {
                    case "Align":
                        d.setAlign(aMessage.Position);
                        break;
                }
            }
        }
        void processMessages() {
            while (IGC.UnicastListener.HasPendingMessage) {
                var msg = IGC.UnicastListener.AcceptMessage();
                if (state == States.ready) {

                    try {
                        switch (msg.Tag) {
                            case "DockMessage":
                                processDockMessage(new DockMessage(msg.Data));
                                break;
                        }
                    } catch (Exception ex) {
                    }
                }
            }
        }
        void Main(string argument, UpdateType aUpdate) {
            if (aUpdate.HasFlag(UpdateType.IGC)) {
                processMessages();
            }
            if (aUpdate.HasFlag(UpdateType.Update10)) {
                try {
                    g.log("Manager state ", state);
                    g.log("docks found ", mListDocks.Count);
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
                            for (int i = 0; i < mListDocks.Count; i++) {
                                if (!mListDocks[i].retract()) {
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
