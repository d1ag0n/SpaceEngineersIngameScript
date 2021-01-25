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
        List<IMyProductionBlock> mProduction;
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
            step = 0;
            index =
            subIndex = 0;

            mGTS.getByTag("lcd", ref mLCD);
            mCargo = new List<IMyEntity>();
            mProduction = new List<IMyProductionBlock>();
            mGTS.getByTag(tagInventory, mCargo);
            for (int i = 0; i < mCargo.Count; i++) {
                var b = mCargo[i];
                if (b is IMyProductionBlock) {
                    var p = b as IMyProductionBlock;
                    p.UseConveyorSystem = false;
                    mProduction.Add(p);
                }
            }
        }

        public void Save() { }

        bool getTag4Item(MyInventoryItem aItem, out string tag) {
            tag = null;
            switch (aItem.Type.TypeId) {
                case "MyObjectBuilder_AmmoMagazine":
                    switch (aItem.Type.SubtypeId) {
                        case "NATO_5p56x45mm":
                            tag = "nato556";
                            break;
                        case "NATO_25x184mm":
                            tag = "nato25";
                            break;
                        case "Missile200mm":
                            tag = "missile";
                            break;
                    }
                    break;
                case "MyObjectBuilder_Component":
                    switch (aItem.Type.SubtypeId) {
                        case "Canvas":
                            tag = "canvas";
                            break;
                    }
                    break;
                case "MyObjectBuilder_Ingot":
                    tag = aItem.Type.SubtypeId.ToLower();
                    break;
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

        void sort(IMyEntity aSourceCargo, IMyInventory aSourceInventory, MyInventoryItem aItem, string aTag) {
            
            g.log("sorting #", aTag);
            var list = new List<IMyEntity>();
            mGTS.getByTag(aTag, list);
            if (list.Count == 0 && !mGTS.hasTag((IMyTerminalBlock)aSourceCargo, tagAnything)) {
                mGTS.getByTag(tagAnything, list);
            }
            MyItemInfo itemInfo;
            if (list.Count != 0) {
                itemInfo = aItem.Type.GetItemInfo();
                for (int i = 0; i < list.Count; i++) {
                    var c = list[i];
                    if (c.EntityId != aSourceCargo.EntityId) {
                        var inv = c.GetInventory();

                        if (aSourceInventory.CanTransferItemTo(inv, aItem.Type)) {
                            var max = (float)inv.MaxVolume;
                            var cur = (float)inv.CurrentVolume;
                            var free = max - cur;
                            var volume = (float)aItem.Amount * itemInfo.Volume;
                            /*
                            if ("missile" == aTag) {
                                g.log("           Max Volume: ", max);
                                g.log("       Current Volume: ", cur);
                                g.log("          Free Volume: ", free);
                                g.log("          Item Volume: ", volume);
                                Me.Enabled = false;
                                Echo(g.clear());
                            }*/
                            if (free > volume) {
                                aSourceInventory.TransferItemTo(inv, aItem);
                            } else {
                                var amt = aItem.Amount;
                                // todo use aItem.Type.GetItemInfo().Volume ??
                                // aSourceInventory.TransferItemTo()
                                //g.log("Cannot transfer #", aTag, " volume ", volume, " cargo free ", free);
                                //g.log("raw amount ", amt.RawValue);
                                
                                if (itemInfo.UsesFractions) {
                                    //g.log("fractional");
                                    amt.RawValue = (long)(free / itemInfo.Volume);
                                } else {
                                    amt.RawValue = (int)(free / itemInfo.Volume);
                                }
                                amt.RawValue *= 1000000;
                                //g.log("plan to transfer ", amt.RawValue);
                                aSourceInventory.TransferItemTo(inv, aItem, amt);
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
        }
        enum Steps
        {
            cargo = 0,
            production,
            done
        }
        Steps step;
        int index = 0;
        int subIndex = 0;
        void sort(IMyEntity aSource, IMyInventory aSourceInventory, MyInventoryItem? aItem) {
            string tag;
            if (aItem.HasValue) {
                if (getTag4Item(aItem.Value, out tag)) {
                    if (!mGTS.hasTag((IMyTerminalBlock)aSource, tag)) {
                        sort(aSource, aSourceInventory, aItem.Value, tag);
                    }
                }
            }
        }
        void stepProduction(IMyProductionBlock aProduction) {
            g.log("Step Production ", aProduction.CustomData);
            var inv = aProduction.OutputInventory;
            if (inv.IsItemAt(subIndex)) {
                sort(aProduction, inv, inv.GetItemAt(subIndex));
                subIndex++;
            } else {
                subIndex = 0;
                index++;
            }
        }
        void stepProduction() {
            if (mProduction.Count > 0 && index < mProduction.Count) {
                g.log("production count ", mProduction.Count);
                stepProduction(mProduction[index]);
            } else {
                g.log("production sort complete");
                index = 0;
                step++;
            }
        }
        void stepCargo(IMyEntity aCargo) {
            MyInventoryItem? item;
            var inv = aCargo.GetInventory();
            if (inv.IsItemAt(subIndex)) {
                sort(aCargo, inv, inv.GetItemAt(subIndex));
                subIndex++;
            } else {
                subIndex = 0;
                index++;
            }
        }
        void stepCargo() {
            if (mCargo.Count > 0 && index < mCargo.Count) {
                stepCargo(mCargo[index]);
            } else {
                index = 0;
                step++;
            }
        }
        void flush() {
            var str = g.clear();
            Echo(str);
            mLCD.WriteText(str);
        }
        public void Main(string argument, UpdateType updateSource) {
            //g.log("Main");
            if (updateSource.HasFlag(UpdateType.Terminal)) {
                Echo("Processing argument: " + argument);
                switch (argument) {
                    case "reinit":
                        reinit();
                        break;
                    case "status":
                        g.log("step ", step);
                        g.log("index ", index);
                        g.log("subIndex ", subIndex);
                        flush();
                        break;
                    default: 
                        Echo("I'm sorry Dave, I'm afraid I can't do that.");
                        break;
                }
            }
            if (updateSource.HasFlag(UpdateType.Update10)) {
                switch (step) {
                    case Steps.cargo:
                        stepCargo();
                        break;
                    case Steps.production:
                        stepProduction();
                        break;
                }
                if (Steps.done == step) {
                    g.log("sorting complete");
                    g.log("index ", index);
                    g.log("subIndex ", subIndex);
                    step = 0;
                    flush();
                }
            }
        }
    }
}
