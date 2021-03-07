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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string tagInventory = "inventory";
        const string tagAnything = "anything";
        const string tagShip = "ship";
        const string tagOrder = "order";
        // ratios - each on hand ingot rawvalue is divided by the ratio, higher values mean lower priority
        const long iron         = 600000000;
        const long cobalt       = 220000000;
        const long nickel       = 70000000;
        const long silicon      = 35000000;
        const long stone        = 20000000;
        const long silver       = 20000000;
        const long gold         = 10000000;
        const long magnesium    = 3000000;
        const long platinum     = 4000000;
        const long uranium      = 100000;

        enum Steps {
            refinerySort = 0,   // sort refinery output into correct locations 
            assemblerSort,      // sort assembler output into correct locations
            cargoSort,          // sort inventory into correct locations
            refineryCount,      // count everything in refinery inputs
            assemblerCount,     // count everything in assembler inputs
            cargoCount,         // count everything in cargo
            resourceRatio,      // calculate ratios for resources
            refine,             // put needed ore into refineries
            //assemble,         // put needed ingot into assemblers
            //produce,          // put needed orders into assemblers

            //inventory,  // look at ship/order tags and generate inventory
            //shipping,   // review fill foreign orders
            //receiving,  // review pending orders
            done
        }

        readonly Logger g;
        readonly GTS mGTS;

        IMyTextPanel mLCD;
        IMyTextPanel mDisplay;
        IMyTextPanel mHelp;

        readonly IMyBroadcastListener listener;

        readonly List<IMyCargoContainer> mCargo = new List<IMyCargoContainer>();
        //readonly List<IMyProductionBlock> mProduction = new List<IMyProductionBlock>();
        readonly List<IMyRefinery> mRefineries = new List<IMyRefinery>();
        readonly List<IMyAssembler> mAssemblers = new List<IMyAssembler>();

        readonly List<IMyTerminalBlock> mWorkList = new List<IMyTerminalBlock>();
        readonly Inventory inventory = new Inventory();
        readonly Dictionary<long, ForeignOrder> foreignOrders = new Dictionary<long, ForeignOrder>();
        //readonly Dictionary<string, long> mInventoryAvailable = new Dictionary<string, long>();
        //readonly Dictionary<string, long> mInventoryRequired = new Dictionary<string, long>();

        readonly Dictionary<long, InventoryOrder> unconfirmedOutgoing = new Dictionary<long, InventoryOrder>();
        readonly Dictionary<string, List<InventoryOrder>> confirmedOutgoing = new Dictionary<string, List<InventoryOrder>>();
        
        readonly List<InventoryOrder> unconfirmedIncoming = new List<InventoryOrder>();        
        readonly Dictionary<string, List<InventoryOrder>> confirmedIncoming = new Dictionary<string, List<InventoryOrder>>();
        readonly SortedDictionary<string, MyFixedPoint> mInventory = new SortedDictionary<string, MyFixedPoint>();
        const int broadcastInterval = 10;

        readonly SortedDictionary<string, string> mToTag = new SortedDictionary<string, string>();

        readonly Dictionary<string, List<InventoryRegister>> mInventoryRegister = new Dictionary<string, List<InventoryRegister>>();

        Steps step;
        int supIndex = 0;
        int index = 0;
        int subIndex = 0;
        
        double lastBroadcast = 0;
        double lastShipment = 0;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            g = new Logger();
            mGTS = new GTS(this, g);
            
            listener = IGC.RegisterBroadcastListener(tagInventory);
            listener.SetMessageCallback(tagInventory);
            init();
        }

        void reinit() {
            mGTS.init();
            init();
        }
        void showHelp() {
            foreach(var p in mToTag) {
                var key = p.Key;
                if (key.StartsWith("MyObjectBuilder_")) {
                    key = key.Substring(16);
                }
                mBuilder.AppendFormat("{0,-40}", key);
                mBuilder.AppendFormat("{0,-20}", p.Value);
                mBuilder.AppendLine();
            }
            mHelp.WriteText(mBuilder.ToString());
            mBuilder.Clear();
        }
        void init() {
            step = 0;
            index =
            subIndex = 0;
            mLCD = null;
            mGTS.getByTag("inventoryConsole", ref mLCD);
            mGTS.getByTag("inventoryDisplay", ref mDisplay);
            mGTS.getByTag("inventoryHelp", ref mHelp);

            initTags();

            if (mHelp != null) {
                showHelp();
            }

            

            mCargo.Clear();
            mGTS.initListByTag(tagInventory, mCargo);

            
            mGTS.initList(mRefineries);
            foreach(var r in mRefineries) {
                r.Enabled = true;
                r.UseConveyorSystem = false;
            }

            mGTS.initList(mAssemblers);
            foreach(var a in mAssemblers) {
                a.Enabled = true;
                a.UseConveyorSystem = false;
                a.Mode = MyAssemblerMode.Assembly;
                a.CooperativeMode = false;
                a.Repeating = false;
                a.ClearQueue();
            }

        }
        public void Save() { }
        void initTags() {
            mToTag["MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm"] = "nato556";
            mToTag["MyObjectBuilder_AmmoMagazine/NATO_25x184mm"] = "nato25";
            mToTag["MyObjectBuilder_AmmoMagazine/Missile200mm"] = "missile";

            mToTag["MyObjectBuilder_Component/SteelPlate"] = "plate";
            mToTag["MyObjectBuilder_Component/SolarCell"] = "solar";
            mToTag["MyObjectBuilder_Component/Girder"] = "girder";
            mToTag["MyObjectBuilder_Component/SmallTube"] = "tube";
            mToTag["MyObjectBuilder_Component/Detector"] = "pipe";
            mToTag["MyObjectBuilder_Component/Reactor"] = "reactor";
            mToTag["MyObjectBuilder_Component/Computer"] = "computer";
            mToTag["MyObjectBuilder_Component/Canvas"] = "canvas";
            mToTag["MyObjectBuilder_Component/Construction"] = "construction";
            mToTag["MyObjectBuilder_Component/InteriorPlate"] = "interior";
            mToTag["MyObjectBuilder_Component/BulletproofGlass"] = "glass";
            mToTag["MyObjectBuilder_Component/MetalGrid"] = "grid";
            mToTag["MyObjectBuilder_Component/GravityGenerator"] = "gravity";
            mToTag["MyObjectBuilder_Component/LargeTube"] = "pipe";
            mToTag["MyObjectBuilder_Component/Superconductor"] = "conductor";
            mToTag["MyObjectBuilder_Component/Display"] = "display";
            mToTag["MyObjectBuilder_Component/RadioCommunication"] = "radio";
            mToTag["MyObjectBuilder_Component/PowerCell"] = "cell";
            mToTag["MyObjectBuilder_Component/Thrust"] = "thrust";
            mToTag["MyObjectBuilder_Component/Medical"] = "medical";
            mToTag["MyObjectBuilder_Component/Motor"] = "motor";

            mToTag["MyObjectBuilder_ConsumableItem/Powerkit"] = "power";

            mToTag["MyObjectBuilder_Datapad/Datapad"] = "data";

            mToTag["MyObjectBuilder_Ingot/Uranium"] = "uranium";
            mToTag["MyObjectBuilder_Ingot/Nickel"] = "nickel";
            mToTag["MyObjectBuilder_Ingot/Platinum"] = "platinum";
            mToTag["MyObjectBuilder_Ingot/Iron"] = "iron";
            mToTag["MyObjectBuilder_Ingot/Magnesium"] = "magnesium";
            mToTag["MyObjectBuilder_Ingot/Gold"] = "gold";
            mToTag["MyObjectBuilder_Ingot/Silver"] = "silver";
            mToTag["MyObjectBuilder_Ingot/Silicon"] = "silicon";
            mToTag["MyObjectBuilder_Ingot/Cobalt"] = "cobalt";
            mToTag["MyObjectBuilder_Ingot/Stone"] = "gravel";

            mToTag["MyObjectBuilder_PhysicalGunObject/Welder2Item"] = "junk";

            mToTag["MyObjectBuilder_PhysicalGunObject/HandDrill4Item"] = "drill";
            mToTag["MyObjectBuilder_PhysicalGunObject/AngleGrinderItem"] = "junk";

            mToTag["MyObjectBuilder_PhysicalObject/SpaceCredit"] = "credit";

            mToTag["MyObjectBuilder_Ore/Organic"] = "organic";
            mToTag["MyObjectBuilder_Ore/Magnesium"] = "magnesiumore";
            mToTag["MyObjectBuilder_Ore/Nickel"] = "nickelore";
            mToTag["MyObjectBuilder_Ore/Platinum"] = "platinumore";
            mToTag["MyObjectBuilder_Ore/Silicon"] = "soliconore";
            mToTag["MyObjectBuilder_Ore/Silver"] = "silverore";
            mToTag["MyObjectBuilder_Ore/Uranium"] = "uraniumore";
            mToTag["MyObjectBuilder_Ore/Gold"] = "goldore";
            mToTag["MyObjectBuilder_Ore/Cobalt"] = "cobaltore";
            mToTag["MyObjectBuilder_Ore/Iron"] = "ironore";
            mToTag["MyObjectBuilder_Ore/Organic"] = "organic";
            mToTag["MyObjectBuilder_Ore/Ice"] = "ice";
            mToTag["MyObjectBuilder_Ore/Scrap"] = "scrap";
            mToTag["MyObjectBuilder_Ore/Stone"] = "stone";

            mToTag["MyObjectBuilder_GasContainerObject/HydrogenBottle"] = "hydrogen";
            mToTag["MyObjectBuilder_OxygenContainerObject/OxygenBottle"] = "oxygen";


        }
        bool getTag4Item(MyInventoryItem aItem, out string tag/*, out string category*/) {
            bool result = mToTag.TryGetValue(aItem.Type.ToString(), out tag);
            if (!result) {
                tag = null;
                g.log("Tag not found for ", aItem.Type);
            }
            return result;
        }
        void sort(IMyTerminalBlock aSourceCargo, IMyInventory aSourceInventory, MyInventoryItem aItem, string aTag) {
            
            //g.log("sorting #", aTag, " from ", aSourceCargo.CustomName);
            
            mGTS.initListByTag(aTag, mWorkList);
            if (mWorkList.Count == 0 && !mGTS.hasTag(aSourceCargo, tagAnything)) {
                mGTS.initListByTag(tagAnything, mWorkList, false);
            }
            MyItemInfo itemInfo;
            if (mWorkList.Count != 0) {
                itemInfo = aItem.Type.GetItemInfo();
                //g.log(mWorkList.Count, " destinations found");
                foreach (var c in mWorkList) {
                    
                    if (c.EntityId != aSourceCargo.EntityId) {
                        //g.log("trying ", c.CustomName);
                        var inv = c.GetInventory();
                        // todo remove this check and just check return value of TransferItemTo
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
                                //g.log("transferring whole chunk");
                                aSourceInventory.TransferItemTo(inv, aItem);
                            } else {
                                //g.log("transferring partial chunk");
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
                        } else {
                            g.log("cannot transfer");
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
                    } else {
                        //g.log("source was same as destination");
                    }
                }
            } else {
                //g.log("no destinations found");
            }
        }
        void sort(IMyTerminalBlock aSource, IMyInventory aSourceInventory, MyInventoryItem aItem) {
            string tag;
            if (getTag4Item(aItem, out tag)) {
                if (!mGTS.hasTag(aSource, tag)) {
                    sort(aSource, aSourceInventory, aItem, tag);
                }
            }
        }
        /// <summary>
        /// sort assemblers and refineries
        /// </summary>
        /// <param name="aProduction"></param>
        void stepProductionSort(IMyProductionBlock aProduction) {
            var inv = aProduction.OutputInventory;
            var okay = inv != null;

            if (okay) {
                var item = inv.GetItemAt(subIndex);
                okay = item.HasValue;
                if (okay) { 
                    sort(aProduction, inv, item.Value);
                    subIndex++;
                }
            }
            if (!okay) {
                subIndex = 0;
                index++;
            }
        }
        void stepRefinerySort() {
            if (mRefineries.Count > 0 && index < mRefineries.Count) {
                stepProductionSort(mRefineries[index]);
            } else {
                g.log("Refineries sorted ", mRefineries.Count);
                g.log("Refinery sort complete");
                index = 0;
                step++;
            }
        }
        void stepAssemblerSort() {
            if (mAssemblers.Count > 0 && index < mAssemblers.Count) {
                stepProductionSort(mAssemblers[index]);
            } else {
                g.log("Assemblers sorted ", mAssemblers.Count);
                index = 0;
                step++;
            }
        }
        struct InventoryRegister {
            public IMyInventory Inventory;
            public MyInventoryItem Item;
            public InventoryRegister(IMyInventory aInventory, MyInventoryItem aItem) {
                Inventory = aInventory;
                Item = aItem;
            }
        }
        void registerInventory(IMyInventory aInventory, MyInventoryItem aItem) {
            List<InventoryRegister> list;
            string tag;

            if (getTag4Item(aItem, out tag)) {
                if (!mInventoryRegister.TryGetValue(tag, out list)) {
                    list = new List<InventoryRegister>();
                    mInventoryRegister.Add(tag, list);
                }
                list.Add(new InventoryRegister(aInventory, aItem));
            }

        }
        void stepCargoSort(IMyCargoContainer aCargo) {
            var inv = aCargo.GetInventory();
            var okay = inv != null;
            if (okay) {
                var item = inv.GetItemAt(subIndex);
                okay = item.HasValue;
                if (okay) {
                    
                    sort(aCargo, inv, item.Value);
                    subIndex++;
                }
            }
            if (!okay) {
                subIndex = 0;
                index++;
            }
        }
        void stepCargoSort() {
            if (mCargo.Count > 0 && index < mCargo.Count) {
                stepCargoSort(mCargo[index]);
            } else {
                index = 0;
                step++;
            }
        }
        /*bool canOrder(string aTag) {
            if (aTag == tagAnything || aTag == tagInventory || aTag == tagOrder || aTag == tagShip) {
                return false;
            }
            return true;
        }
        void stepInventory(IMyEntity aCargo) {
            var cc = (IMyCargoContainer)aCargo;
            if (mGTS.hasTag(cc, tagOrder)) {
                var tags = mGTS.getTags(cc);
                foreach (var tag in tags) {
                    if (canOrder(tag)) {
                        var inv = cc.GetInventory();
                        var max = inv.MaxVolume;
                        var cur = inv.CurrentVolume;
                        var free = max - cur;
                        List<InventoryOrder> orders;
                        if (confirmedIncoming.TryGetValue(tag, out orders)) {
                            foreach (var order in orders) {
                                free.RawValue -= order.Volume;
                            }
                        }
                        if (free > 0) {
                            inventory.AddForOrdering(tag, free);
                        }
                        // todo currently only can order one tag per container
                        // todo need to handle inventory % by maxvolume/#ofTags
                        break;
                    }
                }
                index++;
            } else if (mGTS.hasTag(cc, tagShip)) {
                var inv = cc.GetInventory();
                if (inv.IsItemAt(subIndex)) {
                    var nitem = inv.GetItemAt(subIndex);
                    if (nitem.HasValue) {
                        var item = nitem.Value;
                        string tag;
                        if (getTag4Item(item, out tag)) {
                            if (canOrder(tag)) {
                                inventory.AddForShipping(tag, item.Amount * item.Type.GetItemInfo().Volume);
                            }
                        }
                    }
                    subIndex++;
                } else {
                    subIndex = 0;
                    index++;
                }
            } else {
                index++;
            }
        }
        void stepInventory() {
            if (mCargo.Count > 0 && index < mCargo.Count) {
                if (0 == index && 0 == subIndex) {
                    inventory.Clear();
                }
                stepInventory(mCargo[index]);
            } else {
                index = 0;
                step++;
            }
        }*/
        void flush() {
            var str = g.clear();
            Echo(str);
            if (null != mLCD) mLCD.WriteText(str);
        }
        /*void receiveMessages() {
            while (listener.HasPendingMessage) {
                var msg = listener.AcceptMessage();
                ForeignOrder fo = null;
                if (!foreignOrders.TryGetValue(msg.Source, out fo)) {
                    foreignOrders[msg.Source] = fo = new ForeignOrder();

                }
                var data = (MyTuple<double, ImmutableDictionary<string, long>>)msg.Data;
                fo.LastShipment = data.Item1;
                fo.Orders = data.Item2;
            }
            while (IGC.UnicastListener.HasPendingMessage) {
                var msg = IGC.UnicastListener.AcceptMessage();
                if (msg.Tag == tagOrder) {
                    unconfirmedIncoming.Add(new InventoryOrder(msg.Data));
                } else if (msg.Tag == tagShip) {
                    var data = (MyTuple<uint, long>)msg.Data;
                    var order = unconfirmedOutgoing[data.Item1];
                    order.Confirmed = true;
                    List<InventoryOrder> list;
                    if (!confirmedOutgoing.TryGetValue(order.Item, out list)) {
                        confirmedOutgoing[order.Item] = list = new List<InventoryOrder>();
                    }
                    if (list.Contains(order)) {
                        throw new Exception();
                    }
                    list.Add(order);
                    unconfirmedOutgoing.Remove(order.Id);
                }
            }
        }*/
        /*
        bool confirmOrder(InventoryOrder aOrder) => IGC.SendUnicastMessage(aOrder.Shipper, tagShip, new MyTuple<uint, long>(aOrder.Id, aOrder.Volume));
        void broadcastOrders() {
            if (MAF.time - lastBroadcast > broadcastInterval) {
                var keys = unconfirmedOutgoing.Keys.ToArray();
                foreach (var key in keys) {
                    var item = unconfirmedOutgoing[key];
                    if (MAF.time - item.Time > broadcastInterval * 2) {
                        item.Order.OpenOrders.Remove(item.Item);
                        unconfirmedOutgoing.Remove(key);
                    }
                }

                IGC.SendBroadcastMessage(tagInventory, inventory.SerializeOrders(lastShipment));
                lastBroadcast = MAF.time;
            }
        }*/
        //bool sendOrder(InventoryOrder aOrder) => IGC.SendUnicastMessage(aOrder.Receiver, tagOrder, aOrder.Serialize());
        /*long getRequired(string aTag) {
            long result;
            
            if (inventory.Ordering.TryGetValue(aTag, out result)) {
                List<InventoryOrder> list;
                if (confirmedIncoming.TryGetValue(aTag, out list)) {
                    foreach (var po in list) {
                        result -= po.Volume;
                    }
                }
            } else {
                result = 0;
            }
            return result;
        }*/
        /*long getAvailable(string aTag) {
            long result;
            if (!mInventoryAvailable.TryGetValue(aTag, out result)) {
                if (inventory.Shipping.TryGetValue(aTag, out result)) {
                    foreach (var unconfirmed in unconfirmedOutgoing) {
                        if (unconfirmed.Value.Item == aTag) {
                            result -= unconfirmed.Value.Volume;
                        }
                    }
                    List<InventoryOrder> list;
                    if (confirmedOutgoing.TryGetValue(aTag, out list)) {
                        foreach (var confirmed in list) {
                            result -= confirmed.Volume;
                        }
                    }
                    
                } else {
                    result = 0;
                }
                mInventoryAvailable[aTag] = result;
            }
            return result;
        }*/
        /*void stepShipping() {
            mInventoryAvailable.Clear();
            foreach (var item in foreignOrders) {
                var source = item.Key;
                var fo = item.Value;

                foreach (var order in fo.Orders) {
                    var tag = order.Key;
                    if (!fo.OpenOrders.ContainsKey(tag)) {

                        var volume = order.Value;

                        var available = getAvailable(tag);

                        if (available > 0) {
                            if (available > volume) {
                                available = volume;
                            }
                            var po = new InventoryOrder();
                            po.Item = tag;
                            po.Receiver = source;
                            po.Shipper = Me.EntityId;
                            po.Time = MAF.time;
                            po.Volume = available;
                            
                            if (sendOrder(po)) {
                                po.Order = fo;
                                fo.OpenOrders.Add(tag, po);
                                unconfirmedOutgoing.Add(po.Id, po);
                            }
                        }
                    }
                }
            }
            
            step++;
        }*/
        /*void stepReceiving() {
            for (int i = unconfirmedIncoming.Count -1; i > -1; i--) {
                var po = unconfirmedIncoming[i];
                unconfirmedIncoming.RemoveAt(i);
                var required = getRequired(po.Item);
                g.persist(
                    "reviewed unconfirmed incoming " + 
                    po.ToString() + " required " + required
                );

                if (required > 0) {
                    if (required < po.Volume) {
                        po.Volume = required;
                    }
                    if (confirmOrder(po)) {
                        List<InventoryOrder> list;
                        if (!confirmedIncoming.TryGetValue(po.Item, out list)) {
                            confirmedIncoming[po.Item] = list = new List<InventoryOrder>();
                        }
                        list.Add(po);
                    }
                }
            }
            step++;
        }*/
        
        void logInventory() {
            if (mDisplay != null) {
                foreach (var p in mInventory) {
                    var key = p.Key;
                    if (key.StartsWith("MyObjectBuilder_")) {
                        key = key.Substring(16);
                    }
                    long amount = (long)p.Value;
                    mBuilder.AppendFormat("{0,-34}", key);
                    mBuilder.AppendFormat("{0,10}-", amount);
                    mBuilder.Append(p.Value.RawValue);
                    mBuilder.AppendLine();
                }
                mDisplay.WriteText(mBuilder.ToString());
                mBuilder.Clear();
            }
            mInventory.Clear();
        }
        readonly StringBuilder mBuilder = new StringBuilder();
        void stepCount(MyInventoryItem aItem) {
            var key = aItem.Type.ToString();
            MyFixedPoint amount;
            if (!mInventory.TryGetValue(key, out amount)) {
                amount = 0;
            }
            mInventory[key] = amount + aItem.Amount;
        }
        void stepCount(IMyInventory aInventory, bool register) {
            var okay = aInventory != null;
            if (okay) {
                var item = aInventory.GetItemAt(subIndex);
                okay = item.HasValue;
                if (okay) {
                    if (register) {
                        registerInventory(aInventory, item.Value);
                    }
                    stepCount(item.Value);
                    subIndex++;
                }
            }
            if (!okay) {
                subIndex = 0;
                index++;
            }
        }

        void stepRefineryCount() {
            if (mRefineries.Count > 0 && index < mRefineries.Count) {
                stepCount(mRefineries[index].InputInventory, false);
            } else {
                index = 0;
                step++;
            }
        }
        void stepAssemblerCount() {
            if (mAssemblers.Count > 0 && index < mAssemblers.Count) {
                stepCount(mAssemblers[index].InputInventory, false);
            } else {
                index = 0;
                step++;
            }
        }
        void clearLists() {
            foreach (var list in mInventoryRegister.Values) {
                list.Clear();
            }
        }

        void stepRefine(IMyRefinery aRefinery) {
            if (subIndex == 0) {
                var inv = aRefinery.InputInventory;
                var item = inv.GetItemAt(0);
                if (item.HasValue) {
                    string tag;
                    var gotTag = getTag4Item(item.Value, out tag);
                    if (gotTag && tag == makeResourceTag) {
                        // todo ensure volume
                        index++;
                    } else {
                        g.log("gotTag=", gotTag);
                        g.log("tag=", tag);
                        g.log("makeResourceTag=", makeResourceTag);
                        sort(aRefinery, inv, item.Value, tag);
                    }
                } else {
                    List<InventoryRegister> list;
                    g.log("make resource tag ", makeResourceTag);
                    
                    if (mInventoryRegister.TryGetValue(makeResourceTag, out list)) {
                        g.log("IR List count ", list.Count);
                        float transfer = 7; // volume to transfer m^3

                        foreach (var ir in list) {
                            var info = ir.Item.Type.GetItemInfo();
                            var kgvol = (float)info.Volume;
                            var kgamt = (float)ir.Item.Amount;
                            var mcavail = kgamt * kgvol;
                            if (mcavail > transfer) {
                                MyFixedPoint mfp = (MyFixedPoint)(transfer / info.Volume);
                                if (ir.Inventory.TransferItemTo(inv, ir.Item, mfp)) {
                                    g.log("full transfer success");
                                    break;
                                }
                            } else {
                                MyFixedPoint mfp = (MyFixedPoint)(mcavail / info.Volume);
                                if (ir.Inventory.TransferItemTo(inv, ir.Item, mfp)) {
                                    g.log("partial transfer success");
                                    transfer -= mcavail;
                                } else {
                                    g.log("partial transfer failure");
                                }
                            }
                            g.log(makeResourceTag, " item amount ", ir.Item.Amount);
                            g.log(makeResourceTag, " item info volume ", info.Volume);
                            g.log(makeResourceTag, " item available volume ", mcavail);

                        }
                        
                    } else {
                        g.log("Could not get IR List");
                    }
                    index++;
                }
            }
        }
        void stepRefine() {
            if (makeResourceTag != null && mRefineries.Count > 0 && index < mRefineries.Count) {
                stepRefine(mRefineries[index]);
            } else {
                index = 0;
                step++;
            }
        }

        void stepCargoCount() {
            if (index == 0 && subIndex == 0) {
                clearLists();
            }
            if (mCargo.Count > 0 && index < mCargo.Count) {
                stepCount(mCargo[index].GetInventory(), true);
            } else {
                index = 0;
                step++;
            }
        }

        void calcResourceRatio(string ingot, string ore, long div, ref long ratio, ref string make) {
            MyFixedPoint value;
            bool assign = false;
            long r = -1;
            if (mInventory.TryGetValue(ingot, out value)) {
                r = value.RawValue / div;
                g.log(ingot, " ratio ", r);
                assign = r < ratio;
            } else {
                assign = true;
            }

            if (assign) {
                if (mInventory.TryGetValue(ore, out value)) {
                    if (value > 0) {
                        make = ore;
                        if (r > -1) {
                            ratio = r;
                        }
                    }
                }
            }
        }

        string makeResource;
        string makeResourceTag;
        void calcResourceRatios() {
            long ratio = long.MaxValue;
            makeResource = null;
            calcResourceRatio("MyObjectBuilder_Ingot/Iron", "MyObjectBuilder_Ore/Iron", iron, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Cobalt", "MyObjectBuilder_Ore/Cobalt", cobalt,ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Nickel", "MyObjectBuilder_Ore/Nickel", nickel, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Silicon", "MyObjectBuilder_Ore/Silicon", silicon, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Stone", "MyObjectBuilder_Ore/Stone", stone, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Silver", "MyObjectBuilder_Ore/Silver", silver, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Gold", "MyObjectBuilder_Ore/Gold", gold, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Magnesium", "MyObjectBuilder_Ore/Magnesium", magnesium, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Platinum", "MyObjectBuilder_Ore/Platinum", platinum, ref ratio, ref makeResource);
            calcResourceRatio("MyObjectBuilder_Ingot/Uranium", "MyObjectBuilder_Ore/Uranium", uranium, ref ratio, ref makeResource);


            if (makeResource == null) {
                makeResourceTag = null;
                g.log("Break out the drill it's mining time.");
            } else {
                makeResourceTag = mToTag[makeResource];
                g.log("Want to refine ", makeResourceTag);
            }

            
        }
        public void Main(string argument, UpdateType aUpdate) {
            bool doFlush = false;
            try {
                // ordering receiveMessages();
                if ((aUpdate & (UpdateType.Terminal)) != 0) {
                    Echo("Processing argument: " + argument);
                    switch (argument) {
                        case "reinit":
                            reinit();
                            break;
                        case "status":
                            g.log("step     ", step);
                            g.log("supIndex ", index);
                            g.log("index    ", index);
                            g.log("subIndex ", subIndex);
                            flush();
                            break;
                        default:
                            Echo("I'm sorry Dave, I'm afraid I can't do that.");
                            break;
                    }
                    return;
                }

                if ((aUpdate & (UpdateType.Update10)) != 0) {
                    //Echo($"index {index} subindex {subIndex}");
                    switch (step) {
                        case Steps.refinerySort:
                            stepRefinerySort();
                            break;
                        case Steps.assemblerSort:
                            stepAssemblerSort();
                            break;
                        case Steps.cargoSort:
                            stepCargoSort();
                            break;
                        case Steps.refineryCount:
                            stepRefineryCount();
                            break;
                        case Steps.assemblerCount:
                            stepAssemblerCount();
                            break;
                        case Steps.cargoCount:
                            stepCargoCount();
                            break;
                        case Steps.resourceRatio:
                            calcResourceRatios();
                            step++;
                            break;
                        case Steps.refine:
                            stepRefine();
                            break;
                            /*case Steps.shipping:
                                stepShipping();
                                break;
                            case Steps.receiving:
                                stepReceiving();
                                break;*/
                    }
                    if (Steps.done == step) {
                        /*
                        broadcastOrders();
                        g.log("sorting complete");
                        g.log("index ", index);
                        g.log("subIndex ", subIndex);
                        g.log("ORDERING");
                        foreach (var i in inventory.Ordering) {
                            g.log($"{i.Key} {getRequired(i.Key)}");
                        }
                        g.log("SHIPPING");
                        foreach (var i in inventory.Shipping) {

                            g.log($"{i.Key} {getAvailable(i.Key)}");
                        }
                        g.log("UNCONFIRMED INCOMING");
                        foreach (var o in unconfirmedIncoming) {
                            g.log(o);
                        }
                        g.log("CONFIRMED INCOMING");
                        foreach (var l in confirmedIncoming) {
                            g.log(l.Key);
                            foreach (var o in l.Value) {
                                g.log(o);
                            }
                        }
                        g.log("UNCONFIRMED OUTGOING");
                        foreach (var o in unconfirmedOutgoing) {
                            g.log(o.Value);
                        }
                        g.log("CONFIRMED OUTGOING");
                        foreach (var l in confirmedOutgoing) {
                            g.log(l.Key);
                            foreach (var o in l.Value) {
                                g.log(o);
                            }
                        }*/
                        logInventory();
                        step = 0;
                        doFlush = true;
                    }
                }
            } catch (Exception ex) {
                g.persist(ex.ToString());
            }
            if (doFlush) {
                flush();
            }
        }
    }
}
