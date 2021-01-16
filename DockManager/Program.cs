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
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            for (int i = blocks.Count - 1; i > -1; i--) {
                var block = blocks[i];
                if (block.CubeGrid == Me.CubeGrid) {
                    if (block is IMyShipConnector) {
                        connectors.Add(block as IMyShipConnector);
                    } else if (block is IMyTextPanel) {
                        lcd = block as IMyTextPanel;
                    }
                }
            }
        }
        void update() {
            log("update start");
            dockStatus();
            log("update complete");
        }
        const char mRowSep = '|';
        const char mColSep = ',';
        const char mFieldChar = ' ';
        void dockStatus() {
            var sb = new StringBuilder();
            var one = false;
            for (int i = connectors.Count - 1; i > -1; i--) {
                var con = connectors[i];

                if (MyShipConnectorStatus.Connectable == con.Status) {
                    if (one) {
                        sb.Append(mRowSep);
                    }
                    sb.Append(serialize(con));
                    one = true;
                }
            }
            var msg = sb.ToString();
            IGC.SendBroadcastMessage("docks", msg);
            log("dock status broadcasted", null, msg);
        }
        string serialize(IMyShipConnector aConnector) {
            var sb = new StringBuilder();
            sb.Append(aConnector.EntityId);
            sb.Append(mColSep);
            sb.Append(aConnector.CustomName.Replace(mColSep, mFieldChar).Replace(mRowSep, mFieldChar));
            sb.Append(mColSep);
            sb.Append(aConnector.WorldMatrix.Translation);
            sb.Append(mColSep);
            sb.Append(aConnector.WorldMatrix.Forward);            
            return sb.ToString();
        }
        public void Save() {
            
        }
        void Main(string argument, UpdateType aUpdate) {
            string str = "";
            if (0 < nonUpdateCalls) {
                log(" * * NON UPDATE CALLS ", nonUpdateCalls);
            }
            if (aUpdate.HasFlag(UpdateType.Update1)) {
                count++;
                if (runEvery == count) {
                    count = 0;
                    sb = new StringBuilder();
                    try {
                        update();
                        if (0 < sb.Length) {
                            str = sb.ToString();
                            lcd.WriteText(str);
                        }
                    } catch (Exception ex) {
                        log(ex);
                        str = sb.ToString();
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
                        sb.AppendLine();
                    } else if (arg is Vector3D) {
                        sb.AppendLine();
                        log((Vector3D)arg);
                    } else {
                        sb.Append(arg.ToString());
                    }
                }
            }
            sb.AppendLine();
        }
        int nonUpdateCalls = 0;
        StringBuilder sb = new StringBuilder();
        const int runEvery = 1000;
        int count = runEvery - 1;
        IMyTextPanel lcd;
        List<IMyShipConnector> connectors = new List<IMyShipConnector>();
    }
}
