using Library;
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
        readonly GTS mGTS;
        IMyShipConnector[] marConnectors;
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            mGTS = new GTS(this);
            lcd = mGTS.get<IMyTextPanel>("lcd");
            mGTS.initBlockList<IMyShipConnector>("con", out marConnectors);
        }
        void update() {
            log("update start");
            dockInfo();
            log("update complete");
        }

        void dockInfo() {
            var list = new Connector[marConnectors.Length];
            int i = 0;
            for (; i < marConnectors.Length; i++) {
                list[i] = new Connector(marConnectors[i]);
            }
            IGC.SendBroadcastMessage("docks", Connector.ToCollection(list));
            log("dock info broadcasted ", i);
        }
        
        public void Save() {
            
        }
        void Main(string argument, UpdateType aUpdate) {
            var str = argument;
            
            if (0 < nonUpdateCalls) {
                log(" * * NON UPDATE CALLS ", nonUpdateCalls);
            }
            if (aUpdate.HasFlag(UpdateType.Update1)) {
                count++;
                if (runEvery == count) {
                    count = 0;
                    mLog = new StringBuilder();
                    try {
                        update();
                        if (0 < mLog.Length) {
                            str = mLog.ToString();
                            lcd.WriteText(str);
                        }
                    } catch (Exception ex) {
                        log(ex);
                        str = mLog.ToString();
                    }
                    Echo(str);
                }
            } else {
                nonUpdateCalls++;
            }
        }
        void log(Vector3D v) => log("X ", v.X, null, "Y ", v.Y, null, "Z ", v.Z);
        void log(params object[] args) {
            if (null != args) {
                for (int i = 0; i < args.Length; i++) {
                    var arg = args[i];
                    if (null == arg) {
                        mLog.AppendLine();
                    } else if (arg is Vector3D) {
                        mLog.AppendLine();
                        log((Vector3D)arg);
                    } else {
                        mLog.Append(arg.ToString());
                    }
                }
            }
            mLog.AppendLine();
        }
        int nonUpdateCalls = 0;
        StringBuilder mLog = new StringBuilder();
        const int runEvery = 100;
        int count = runEvery - 1;
        readonly IMyTextPanel lcd;
    }
}
