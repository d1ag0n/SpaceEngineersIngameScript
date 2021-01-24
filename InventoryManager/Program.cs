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
        const string tagInventory = "inventory";
        
        const string tagAnything = "anything";

        readonly GTS mGTS;

        List<IMyEntity> mCargo;
        IMyTextPanel mLCD;
        readonly Logger g;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            g.log("Program construct");
            mGTS = new GTS(this, g);
            init();
        }
        void reinit() {
            mGTS.init();
            init();
        }
        void init() {
            mGTS.get("lcd", out mLCD);
            mCargo = new List<IMyEntity>();
            mGTS.getByTag(tagInventory, mCargo);
        }

        public void Save() { }

        bool getTag4Item(MyInventoryItem aItem, out string tag) {
            tag = null;
            switch (aItem.Type.TypeId) {
                case "MyObjectBuilder_Ore":
                    switch (aItem.Type.SubtypeId) {
                        case "Organic":
                            tag = "organic";
                            break;
                        case "Ice":
                            tag = "ice";
                            break;
                        case "Scrap":
                            tag = "scrap";
                            break;
                        case "Magnesium":
                        case "Nickel":
                        case "Platinum":
                        case "Silicon":
                        case "Silver":
                        case "Uranium":
                        case "Gold":
                        case "Cobalt":
                        case "Iron":
                            tag = aItem.Type.SubtypeId.ToLower() + "ore";
                            break;
                        case "Stone":
                            tag = "stone";
                            break;
                    }
                    break;
            }
            bool result = true;
            if (tag == null) {
                result = false;
                g.log("Tag not found ", aItem.Type);
            }
            return result;
        }
        bool volume(string aTag, out double aVolume) {
            bool result = true;

            switch (aTag) {
                case "scrap":
                    aVolume = 0.000254;
                    break;
                case "organic":
                case "magnesiumore":
                case "nickelore":
                case "platinumore":
                case "siliconore":
                case "silverore":
                case "uraniumore":
                case "ice":
                case "goldore":
                case "cobaltore":
                case "ironore":
                case "stone": 
                    aVolume = 0.00037; 
                    break;
                default: 
                    aVolume = 0.0;
                    result = false;
                    g.log("Volume not found #", aTag);
                    break;
            }

            return result;
        }


        void sort(IMyEntity aSourceCargo, IMyInventory aSourceInventory, MyInventoryItem aItem, string aTag) {
            
            g.log("sorting ", aTag);
            var list = new List<IMyEntity>();
            mGTS.getByTag(aTag, list);
            if (list.Count == 0) {
                mGTS.getByTag(tagAnything, list);
            }
            for (int i = 0; i < list.Count; i++) {
                var c = list[i];
                if (c.EntityId != aSourceCargo.EntityId) {
                    var inv = c.GetInventory();
                    
                    if (aSourceInventory.CanTransferItemTo(inv, aItem.Type)) {
                        var max = inv.MaxVolume;
                        var cur = inv.CurrentVolume;
                        var free = max - cur;
                        double volumeFactor;
                        if (volume(aTag, out volumeFactor)) {
                            var itemVolume = aItem.Amount.RawValue * volumeFactor;
                            if (free.RawValue > itemVolume) {
                                aSourceInventory.TransferItemTo(inv, aItem);
                            }
                        }
                    }
                    //g.log("           Max Volume: ", max);
                    //g.log("       Raw Max Volume: ", max.RawValue);
                    //g.log("       Current Volume: ", cur);
                    //g.log("   Raw Current Volume: ", cur.RawValue);
                    //g.log("          Free Volume: ", free);
                    //g.log("      Raw Free Volume: ", free.RawValue);
                    //g.log("          Item amount: ", aItem.Amount);
                    //g.log("      Raw item amount: ", aItem.Amount.RawValue);
                    //g.log("Raw Calculated Volume: ", aItem.Amount.RawValue * volume(aTag));

                }
            }
        }

        int index = 0;
        int subIndex = 0;
        public void Main(string argument, UpdateType updateSource) {
            string str = null;
            //g.log("Main");
            if (updateSource.HasFlag(UpdateType.Terminal)) {
                Echo("Processing argument: " + argument);
                switch (argument) {
                    case "reinit":
                        reinit();
                        break;
                    default: 
                        Echo("I'm sorry Dave, I'm afraid I can't do that.");
                        break;
                }
            }
            if (updateSource.HasFlag(UpdateType.Update10)) {
                //g.log("mCargo ", mCargo.Count);
                if (index < mCargo.Count) {
                    var c = mCargo[index];
                    var inv = c.GetInventory();
                    //g.log(c.CustomName, " volume ", inv.CurrentVolume);
                    MyInventoryItem? item;
                    if (inv.IsItemAt(subIndex)) {
                        item = inv.GetItemAt(subIndex);
                        if (item.HasValue) {
                            //g.log("TypeId ", item.Value.Type.TypeId);
                            //g.log("SubtypeId ", item.Value.Type.SubtypeId);
                            string tag;
                            if (getTag4Item(item.Value, out tag)) {
                                if (!mGTS.hasTag((IMyTerminalBlock)c, tag)) {
                                    sort(c, inv, item.Value, tag);
                                }
                            }
                        }
                        subIndex++;
                    } else {
                        subIndex = 0;
                        index++;
                    }
                } else {
                    index = 0;
                    str = g.clear();
                    Echo(str);
                    mLCD.WriteText(str);
                }
            }
        }
    }
}
