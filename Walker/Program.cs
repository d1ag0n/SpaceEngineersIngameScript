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
        const string NAME_PISTON_TOP = "PISTON_TOP";
        const string NAME_PISTON_BOTTOM = "PISTON_BOTTOM";
        const string NAME_PISTON_RIGHT = "PISTON_RIGHT";
        const string NAME_PISTON_LEFT = "PISTON_LEFT";
        const string NAME_MIDDLE = "MIDDLE";
        const string NAME_REFERENCE = "REFERENCE";
        const string HAME_HINGE_INTERNAL_TOP = "HAME_HINGE_INTERNAL_TOP";
        const string HAME_HINGE_INTERNAL_BOTTOM = "HAME_HINGE_INTERNAL_BOTTOM";
        const string HAME_HINGE_INTERNAL_LEFT = "HAME_HINGE_INTERNAL_LEFT";
        const string HAME_HINGE_INTERNAL_RIGHT = "HAME_HINGE_INTERNAL_RIGHT";
        double RPM = 1;
        double INC {
            get {
                return (RPM / 60d) / MathHelperD.TwoPi;
            }
        }
        double R = 2.5;
        readonly IMyTerminalBlock REFERENCE;
        readonly IMyTerminalBlock MIDDLE;
        readonly IMyPistonBase PISTON_TOP;
        readonly IMyPistonBase PISTON_BOTTOM;
        readonly IMyPistonBase PISTON_RIGHT;
        readonly IMyPistonBase PISTON_LEFT;

        public Program() {
            PISTON_TOP = GridTerminalSystem.GetBlockWithName(NAME_PISTON_TOP) as IMyPistonBase;
            PISTON_BOTTOM = GridTerminalSystem.GetBlockWithName(NAME_PISTON_BOTTOM) as IMyPistonBase;
            PISTON_RIGHT = GridTerminalSystem.GetBlockWithName(NAME_PISTON_RIGHT) as IMyPistonBase;
            PISTON_LEFT = GridTerminalSystem.GetBlockWithName(NAME_PISTON_LEFT) as IMyPistonBase;
            REFERENCE = GridTerminalSystem.GetBlockWithName(NAME_REFERENCE);
            MIDDLE = GridTerminalSystem.GetBlockWithName(NAME_MIDDLE);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        double angle = 0;

        public void Main(string argument, UpdateType updateSource) {
            if (argument == "faster") {
                RPM += 1;
            } else if (argument == "slower") {
                RPM -= 1;
            } else if (argument == "bigger") {
                R += 0.1;
            } else if (argument == "smaller") {
                R -= 0.1;
            }

            Vector3D vec = MAF.world2pos(MIDDLE.WorldMatrix.Translation, REFERENCE.WorldMatrix);
            
            angle += INC;
            if (angle > MathHelperD.TwoPi) {
                angle -= MathHelperD.TwoPi;
            }

            var x = Math.Cos(angle) * R;
            var y = Math.Sin(angle) * R;

            PISTON_TOP.Velocity = (float)(vec.Y - y) * 10f;
            PISTON_BOTTOM.Velocity = (float)(vec.Y - y) * -10f;

            PISTON_RIGHT.Velocity = (float)(vec.X - x) * 10f;
            PISTON_LEFT.Velocity = (float)(vec.X - x) * -10f;

   

            float dif;
            if (PISTON_LEFT.CurrentPosition > PISTON_RIGHT.CurrentPosition) {
                dif = PISTON_LEFT.CurrentPosition - PISTON_RIGHT.CurrentPosition;
                PISTON_LEFT.Velocity -= dif / 2;
            } else {
                dif = PISTON_RIGHT.CurrentPosition - PISTON_LEFT.CurrentPosition;
            }



            //Echo($"x:{x:f4}");
            //Echo($"y:{y:f4}");
            //Echo($"0:{angle:f4}");
            Echo($"RPM {RPM}");
        }
    }
}
