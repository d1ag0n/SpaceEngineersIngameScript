using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace IngameScript {
    partial class Program : MyGridProgram {
        IMyRadioAntenna Antenna;
        IMyTextPanel Debug;
        IMyBroadcastListener Listener;
        bool SendingBroadcast = false;
        string tag = "1234";
        int count = 0;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            Antenna = (IMyRadioAntenna)GridTerminalSystem.GetBlockWithName("Antenna");
            Debug = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Debug");
            Debug.ContentType = ContentType.TEXT_AND_IMAGE;
            Listener = IGC.RegisterBroadcastListener(tag);

        }


        public void Main(string argument, UpdateType updateSource) {

            Echo(count.ToString());
            Debug.WriteText("\nCount: " + count);
            Debug.WriteText("\nSendingBroadcast: " + SendingBroadcast, true);
            if (SendingBroadcast) {
                Antenna.EnableBroadcasting = false;
                SendingBroadcast = false;
            } else {
                Antenna.EnableBroadcasting = true;
                IGC.SendBroadcastMessage(tag, count);
                count++;
                SendingBroadcast = true;
                Debug.WriteText("\nSent: " + count, true);
            }


            while (Listener.HasPendingMessage) {
                var message = Listener.AcceptMessage();

                Debug.WriteText("\nReceived " + message.Data, true);
            }

        }
    }
}
