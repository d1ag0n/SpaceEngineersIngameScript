﻿using Sandbox.Game.EntityComponents;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram {
        struct InventoryRegister {
            public IMyInventory Inventory;
            public MyInventoryItem Item;
            public InventoryRegister(IMyInventory aInventory, MyInventoryItem aItem) {
                Inventory = aInventory;
                Item = aItem;
            }
        }
        const string tagInventory = "inventory";
        const string tagAnything = "anything";
        const string tagShip = "ship";
        const string tagOrder = "order";
        // ratios - each on hand ingot rawvalue is divided by the ratio, higher values mean lower inventory
        readonly ResourceInfo iron = new ResourceInfo("ironore", "iron", 600000000, MyItemType.MakeIngot("Iron"), 18.2f);
        readonly ResourceInfo cobalt = new ResourceInfo("cobaltore", "cobalt", 220000000, MyItemType.MakeIngot("Cobalt"), 0.13f);
        readonly ResourceInfo nickel = new ResourceInfo("nickelore", "nickel", 70000000, MyItemType.MakeIngot("Nickel"), 1.97f);
        readonly ResourceInfo silicon = new ResourceInfo("siliconore", "silicon", 35000000, MyItemType.MakeIngot("Silicon"), 2.167f);
        readonly ResourceInfo stone = new ResourceInfo("stone", "gravel", 20000000, MyItemType.MakeIngot("Stone"), 130f);
        readonly ResourceInfo silver = new ResourceInfo("silverore", "silver", 20000000, MyItemType.MakeIngot("Silver"), 1.3f);
        readonly ResourceInfo gold = new ResourceInfo("goldore", "gold", 10000000, MyItemType.MakeIngot("Gold"), 3.25f);
        readonly ResourceInfo uranium = new ResourceInfo("uraniumore", "uranium", 10000000, MyItemType.MakeIngot("Uranium"), 0.325f);
        readonly ResourceInfo magnesium = new ResourceInfo("magnesiumore", "magnesium", 3000000, MyItemType.MakeIngot("Magnesium"), 2.6f);
        readonly ResourceInfo platinum = new ResourceInfo("platinumore", "platinum", 4000000, MyItemType.MakeIngot("Platinum"), 0.433f);


        class ResourceInfo {
            
            static double Total = 0;
            public readonly string Ore;
            public readonly string Ingot;
            public readonly long lRatio;
            public readonly float mKGs;
            public double dRatio => lRatio / Total;
            public readonly MyItemType IngotType;
            public long TempRatio;

            public ResourceInfo(string aOre, string aIngot, long alRatio, MyItemType aType, float aKGs) {
                Ore = aOre;
                Ingot = aIngot;
                lRatio = alRatio;
                Total += lRatio;
                IngotType = aType;
                mKGs = aKGs;
            }
        }
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

        readonly LogModule mLog;
        readonly ModuleManager mManager;

        //IMyTextPanel mLCD;
        IMyTextPanel mDisplay;
        IMyTextPanel mHelp;

        //readonly IMyBroadcastListener listener;

        readonly List<IMyCargoContainer> mCargo = new List<IMyCargoContainer>();
        //readonly List<IMyProductionBlock> mProduction = new List<IMyProductionBlock>();
        readonly List<IMyRefinery> mRefineries = new List<IMyRefinery>();
        readonly List<IMyAssembler> mAssemblers = new List<IMyAssembler>();


        //readonly Inventory inventory = new Inventory();
        //readonly Dictionary<long, ForeignOrder> foreignOrders = new Dictionary<long, ForeignOrder>();
        //readonly Dictionary<string, long> mInventoryAvailable = new Dictionary<string, long>();
        //readonly Dictionary<string, long> mInventoryRequired = new Dictionary<string, long>();

        //readonly Dictionary<long, InventoryOrder> unconfirmedOutgoing = new Dictionary<long, InventoryOrder>();
        //readonly Dictionary<string, List<InventoryOrder>> confirmedOutgoing = new Dictionary<string, List<InventoryOrder>>();

        //readonly List<InventoryOrder> unconfirmedIncoming = new List<InventoryOrder>();        
        //readonly Dictionary<string, List<InventoryOrder>> confirmedIncoming = new Dictionary<string, List<InventoryOrder>>();
        readonly SortedDictionary<string, MyFixedPoint> mInventory = new SortedDictionary<string, MyFixedPoint>();
        //readonly Dictionary<string, MyFixedPoint> mInventory = new Dictionary<string, MyFixedPoint>();
        //const int broadcastInterval = 10;

        readonly ImmutableSortedDictionary<string, string> mToTag;
        readonly ImmutableSortedDictionary<string, string> mFromTag;

        readonly Dictionary<string, List<InventoryRegister>> mInventoryRegister = new Dictionary<string, List<InventoryRegister>>();

        // Steps step;
        // int supIndex = 0;
        // int index = 0;
        // int subIndex = 0;

        // double lastBroadcast = 0;
        // double lastShipment = 0;
        readonly ImmutableDictionary<string, ResourceInfo> mResource;
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            mManager = new ModuleManager(this, "Inventory Manager", "inventoryLogConsole", 50);
            mLog = mManager.mLog;

            mResource = (new Dictionary<string, ResourceInfo>() {
                { iron.Ingot, iron },
                { cobalt.Ingot, cobalt },
                { nickel.Ingot, nickel },
                { silicon.Ingot, silicon },
                { stone.Ingot, stone },
                { silver.Ingot, silver },
                { gold.Ingot, gold },
                { magnesium.Ingot, magnesium },
                { platinum.Ingot, platinum },
                { uranium.Ingot, uranium }
            }).ToImmutableDictionary();



            //listener = IGC.RegisterBroadcastListener(tagInventory);
            //listener.SetMessageCallback(tagInventory);
            mManager.Initialize();
            initTags(ref mToTag, ref mFromTag);
            init();
            
            // "priming" all the machines with an initial movenext to they get JIT
            processMachine = process();

            preSortMachine = preSort();
            preSortMachine.MoveNext();

            sortMachine = sort();
            sortMachine.MoveNext();

            stepProductionSortMachine = stepProductionSort();
            stepProductionSortMachine.MoveNext();

            stepRefinerySortMachine = stepRefinerySort();
            stepRefinerySortMachine.MoveNext();

            stepAssemblerSortMachine = stepAssemblerSort();
            stepAssemblerSortMachine.MoveNext();

            stepCargoSortContainerMachine = stepCargoSortContainer();
            stepCargoSortContainerMachine.MoveNext();

            stepCargoSortMachine = stepCargoSort();
            stepCargoSortMachine.MoveNext();

            stepCountMachine = stepCount();
            stepCountMachine.MoveNext();

            stepRefineryCountMachine = stepRefineryCount();
            stepRefineryCountMachine.MoveNext();

            stepAssemblerCountMachine = stepAssemblerCount();
            stepAssemblerCountMachine.MoveNext();

            stepCargoCountMachine = stepCargoCount();
            stepCargoCountMachine.MoveNext();

            stepRefineMachine = stepRefine();
            stepRefineMachine.MoveNext();

            calcResourceRatiosMachine = calcResourceRatios();
            calcResourceRatiosMachine.MoveNext();

            stepAssemblerStockMachine = stepAssemblerStock();
            stepAssemblerStockMachine.MoveNext();
        }


        void showHelp() {
            foreach (var p in mToTag) {
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
            //mLCD = null;
            //mManager.getByTag("inventoryConsole", ref mLCD);
            mManager.getByTag("inventoryDisplay", ref mDisplay);
            mManager.getByTag("inventoryHelp", ref mHelp);

           

            if (mHelp != null) {
                showHelp();
            }

            mCargo.Clear();
            mManager.getByTag(tagInventory, mCargo);

            mManager.getByType(mRefineries);
            foreach (var r in mRefineries) {
                r.Enabled = true;
                r.UseConveyorSystem = false;
            }

            mManager.getByType(mAssemblers);
            foreach (var a in mAssemblers) {
                a.Enabled = true;
                a.UseConveyorSystem = false;
                a.Mode = MyAssemblerMode.Assembly;
                a.CooperativeMode = false;
                a.Repeating = false;
                a.ClearQueue();
            }

        }
        void initTags(ref ImmutableSortedDictionary<string, string> aToTag, ref ImmutableSortedDictionary<string, string> aFromTag) {
            var d = new Dictionary<string, string>();
            d["MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm"] = "nato556";
            d["MyObjectBuilder_AmmoMagazine/NATO_25x184mm"] = "nato25";
            d["MyObjectBuilder_AmmoMagazine/Missile200mm"] = "missile";
            d["MyObjectBuilder_Component/SteelPlate"] = "plate";
            d["MyObjectBuilder_Component/SolarCell"] = "solar";
            d["MyObjectBuilder_Component/Girder"] = "girder";
            d["MyObjectBuilder_Component/SmallTube"] = "tube";
            d["MyObjectBuilder_Component/Detector"] = "detector";
            d["MyObjectBuilder_Component/Reactor"] = "reactor";
            d["MyObjectBuilder_Component/Computer"] = "computer";
            d["MyObjectBuilder_Component/Canvas"] = "canvas";
            d["MyObjectBuilder_Component/Construction"] = "construction";
            d["MyObjectBuilder_Component/InteriorPlate"] = "interior";
            d["MyObjectBuilder_Component/BulletproofGlass"] = "glass";
            d["MyObjectBuilder_Component/MetalGrid"] = "grid";
            d["MyObjectBuilder_Component/GravityGenerator"] = "gravity";
            d["MyObjectBuilder_Component/LargeTube"] = "pipe";
            d["MyObjectBuilder_Component/Superconductor"] = "conductor";
            d["MyObjectBuilder_Component/Display"] = "display";
            d["MyObjectBuilder_Component/RadioCommunication"] = "radio";
            d["MyObjectBuilder_Component/PowerCell"] = "cell";
            d["MyObjectBuilder_Component/Thrust"] = "thrust";
            d["MyObjectBuilder_Component/Medical"] = "medical";
            d["MyObjectBuilder_Component/Motor"] = "motor";
            d["MyObjectBuilder_ConsumableItem/Powerkit"] = "power";
            d["MyObjectBuilder_Datapad/Datapad"] = "data";
            d["MyObjectBuilder_Ingot/Uranium"] = "uranium";
            d["MyObjectBuilder_Ingot/Nickel"] = "nickel";
            d["MyObjectBuilder_Ingot/Platinum"] = "platinum";
            d["MyObjectBuilder_Ingot/Iron"] = "iron";
            d["MyObjectBuilder_Ingot/Magnesium"] = "magnesium";
            d["MyObjectBuilder_Ingot/Gold"] = "gold";
            d["MyObjectBuilder_Ingot/Silver"] = "silver";
            d["MyObjectBuilder_Ingot/Silicon"] = "silicon";
            d["MyObjectBuilder_Ingot/Cobalt"] = "cobalt";
            d["MyObjectBuilder_Ingot/Stone"] = "gravel";
            d["MyObjectBuilder_PhysicalGunObject/Welder2Item"] = "junk";
            d["MyObjectBuilder_PhysicalGunObject/HandDrill4Item"] = "drill";
            d["MyObjectBuilder_PhysicalGunObject/AngleGrinderItem"] = "junk";
            d["MyObjectBuilder_PhysicalObject/SpaceCredit"] = "credit";
            d["MyObjectBuilder_Ore/Organic"] = "organic";
            d["MyObjectBuilder_Ore/Magnesium"] = "magnesiumore";
            d["MyObjectBuilder_Ore/Nickel"] = "nickelore";
            d["MyObjectBuilder_Ore/Platinum"] = "platinumore";
            d["MyObjectBuilder_Ore/Silicon"] = "siliconore";
            d["MyObjectBuilder_Ore/Silver"] = "silverore";
            d["MyObjectBuilder_Ore/Uranium"] = "uraniumore";
            d["MyObjectBuilder_Ore/Gold"] = "goldore";
            d["MyObjectBuilder_Ore/Cobalt"] = "cobaltore";
            d["MyObjectBuilder_Ore/Iron"] = "ironore";
            d["MyObjectBuilder_Ore/Organic"] = "organic";
            d["MyObjectBuilder_Ore/Ice"] = "ice";
            d["MyObjectBuilder_Ore/Scrap"] = "scrap";
            d["MyObjectBuilder_Ore/Stone"] = "stone";
            d["MyObjectBuilder_GasContainerObject/HydrogenBottle"] = "hydrogen";
            d["MyObjectBuilder_OxygenContainerObject/OxygenBottle"] = "oxygen";
            aToTag = d.ToImmutableSortedDictionary();

            d.Clear();
            foreach (var kvp in aToTag) {
                d[kvp.Value] = kvp.Key;
            }
            aFromTag = d.ToImmutableSortedDictionary();
        }
        bool getTag4Item(string aItem, out string tag) {
            return mToTag.TryGetValue(aItem, out tag);
        }
        bool getTag4Item(MyInventoryItem aItem, out string tag/*, out string category*/) {
            bool result = mToTag.TryGetValue(aItem.Type.ToString(), out tag);
            if (!result) {
                tag = null;
                mLog.persist($"Tag not found for {aItem.Type}");
            }

            return result;
        }
        readonly IEnumerator<bool> sortMachine;
        IMyTerminalBlock sortBlock;
        IMyInventory sortInventory;
        MyInventoryItem sortItem;
        string sortTag;
        IEnumerator<bool> sort(/*IMyTerminalBlock aSourceCargo, IMyInventory aSourceInventory, MyInventoryItem aItem, string aTag*/) {
            MyItemInfo itemInfo;
            List<IMyTerminalBlock> workList = new List<IMyTerminalBlock>();
            yield return true;
            while (true) {

                mManager.getByTag(sortTag, workList);
                if (workList.Count == 0 && !mManager.hasTag(sortBlock, tagAnything)) {
                    mManager.getByTag(tagAnything, workList);
                }
                if (workList.Count != 0) {
                    itemInfo = sortItem.Type.GetItemInfo();
                    foreach (var c in workList) {
                        if (c.EntityId != sortBlock.EntityId) {
                            var inv = c.GetInventory();
                            var max = (float)inv.MaxVolume;
                            var cur = (float)inv.CurrentVolume;
                            var free = max - cur;
                            //mLog.persist($"{c.CustomName} {free}m3 free");
                            
                            var volume = (float)sortItem.Amount * itemInfo.Volume;
                            if (free > volume) {
                                if (sortInventory.TransferItemTo(inv, sortItem)) {
                                    //mLog.persist($"transferred stack of {sortTag} from {sortBlock.CustomName} to {c.CustomName}");
                                    break;
                                } else {
                                    //mLog.persist($"failed to transfer stack of {sortTag} from {sortBlock.CustomName} to {c.CustomName}");
                                }
                            } else {
                                var amt = sortItem.Amount;
                                if (itemInfo.UsesFractions) {
                                    amt = (MyFixedPoint)(free / itemInfo.Volume);
                                } else {
                                    continue;
                                }
                                amt.RawValue *= 1000000;
                                if (sortInventory.TransferItemTo(inv, sortItem, amt)) {
                                    mLog.persist($"transferred some {sortTag} from {sortBlock.CustomName} to {c.CustomName}");
                                } else {
                                    mLog.persist($"failed to transfer some {sortTag} from {sortBlock.CustomName} to {c.CustomName}");
                                }
                            }
                        }
                        yield return true;
                    }
                    workList.Clear();
                }
                yield return false;
            }
        }
        IEnumerator<bool> preSortMachine;
        IMyTerminalBlock preSortBlock;
        IMyInventory preSortInventory;
        MyInventoryItem preSortItem;
        IEnumerator<bool> preSort(/*IMyTerminalBlock aSource, IMyInventory aSourceInventory, MyInventoryItem aItem*/) {
            yield return true;
            string tag;
            while (true) {
                if (getTag4Item(preSortItem, out tag)) {
                    if (!mManager.hasTag(preSortBlock, tag)) {
                        sortBlock = preSortBlock;
                        sortInventory = preSortInventory;
                        sortItem = preSortItem;
                        sortTag = tag;
                        while (sortMachine.MoveNext() && sortMachine.Current) {
                            yield return true;
                        }
                    }
                }
                yield return false;
            }
        }

        readonly IEnumerator<bool> stepProductionSortMachine;
        IMyProductionBlock stepProductionSortBlock;
        IEnumerator<bool> stepProductionSort() {
            List<MyInventoryItem> list = new List<MyInventoryItem>();
            yield return true;
            while (true) {
                preSortBlock = stepProductionSortBlock;
                preSortInventory = stepProductionSortBlock.OutputInventory;
                preSortInventory.GetItems(list);
                foreach (var item in list) {
                    preSortItem = item;
                    while (preSortMachine.MoveNext() && preSortMachine.Current) {
                        yield return true;
                    }
                }
                list.Clear();
                yield return false;
            }
        }
        readonly IEnumerator<bool> stepAssemblerSortMachine;
        IEnumerator<bool> stepAssemblerSort() {
            yield return true;
            while (true) {
                int count = mAssemblers.Count;
                int last = count - 1;
                for (int i = 0; i < count; i++) {
                    stepProductionSortBlock = mAssemblers[i];
                    while (stepProductionSortMachine.MoveNext() && stepProductionSortMachine.Current) {
                        yield return true;
                    }
                    if (i < last) {
                        yield return true;
                    }
                }
                yield return false;
            }
        }
        readonly IEnumerator<bool> stepRefinerySortMachine;
        IEnumerator<bool> stepRefinerySort() {
            yield return true;
            while (true) {
                foreach (var r in mRefineries) {
                    stepProductionSortBlock = r;
                    while (stepProductionSortMachine.MoveNext() && stepProductionSortMachine.Current) {
                        yield return true;
                    }
                }
                yield return false;
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
        IMyCargoContainer stepCargoSortBlock;
        readonly IEnumerator<bool> stepCargoSortContainerMachine;
        IEnumerator<bool> stepCargoSortContainer() {
            yield return true;
            while (true) {
                var inv = stepCargoSortBlock.GetInventory();
                if (inv != null) {
                    int count = inv.ItemCount - 1;
                    for (; count > -1; count--) {
                        var item = inv.GetItemAt(count);
                        // todo check here if item is allowed?
                        if (item.HasValue) {
                            preSortBlock = stepCargoSortBlock;
                            preSortInventory = inv;
                            preSortItem = item.Value;
                            while (preSortMachine.MoveNext() && preSortMachine.Current) {
                                yield return true;
                            }
                        } else {
                            break;
                        }
                    }
                }
                yield return false;
            }
        }
        readonly IEnumerator<bool> stepCargoSortMachine;
        IEnumerator<bool> stepCargoSort() {
            yield return true;
            while (true) {
                int count = mCargo.Count;
                int last = count - 1;
                foreach (var c in mCargo) {
                    stepCargoSortBlock = c;
                    while (stepCargoSortContainerMachine.MoveNext() && stepCargoSortContainerMachine.Current) {
                        yield return true;
                    }
                }
                yield return false;
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


        readonly StringBuilder mBuilder = new StringBuilder();
        void stepCount(MyInventoryItem aItem) {
            var key = aItem.Type.ToString();
            MyFixedPoint amount;
            if (!mInventory.TryGetValue(key, out amount)) {
                amount = 0;
            }
            mInventory[key] = amount + aItem.Amount;
        }
        readonly IEnumerator<bool> stepCountMachine;
        IMyInventory stepCountInventory;
        bool stepCountRegister;
        IEnumerator<bool> stepCount(/*IMyInventory aInventory, bool register*/) {
            yield return true;
            while (true) {
                int count = stepCountInventory.ItemCount - 1;
                yield return true;
                for (; count > -1; count--) {
                    var item = stepCountInventory.GetItemAt(count);
                    yield return true;
                    if (item.HasValue) {
                        if (stepCountRegister) {
                            registerInventory(stepCountInventory, item.Value);
                            yield return true;
                        }
                        stepCount(item.Value);
                        yield return true;
                    }
                }
                yield return false;
            }
        }
        IEnumerator<bool> stepRefineryCountMachine;
        IEnumerator<bool> stepRefineryCount() {
            yield return true;
            while (true) {
                foreach (var r in mRefineries) {
                    stepCountInventory = r.InputInventory;
                    stepCountRegister = false;
                    while (stepCountMachine.MoveNext() && stepCountMachine.Current) {
                        yield return true;
                    }
                }
                yield return false;
            }
        }
        IEnumerator<bool> stepAssemblerCountMachine;
        IEnumerator<bool> stepAssemblerCount() {
            yield return true;
            while (true) {
                foreach (var a in mAssemblers) {
                    stepCountInventory = a.InputInventory;
                    stepCountRegister = false;
                    while (stepCountMachine.MoveNext() && stepCountMachine.Current) {
                        yield return true;
                    }
                }
                yield return false;
            }
        }
        float getAmount(MyItemInfo info, float volume) => volume / info.Volume;
        float getVolume(MyItemInfo info, float amount) => amount * info.Volume;

        readonly List<MyItemType> mWorkItemType = new List<MyItemType>();
        readonly Dictionary<string, MyInventoryItem> mWorkInventoryItem = new Dictionary<string, MyInventoryItem>();
        IEnumerator<bool> stepRefineMachine;
        IEnumerator<bool> stepRefine(/*IMyRefinery aRefinery*/) {
            yield return true;
            while (true) {
                // todo clean this up
                foreach (var r in mRefineries) {
                    var inv = r.InputInventory;
                    var maxVol = ((float)inv.MaxVolume * 0.9f);
                    mLog.persist($"maxVol={maxVol} for {r.CustomName}");
                    var preferredVolume = maxVol / mResource.Count;
                    inv.GetItems(null, i => {
                        mWorkInventoryItem[i.Type.ToString()] = i;
                        return false;
                    });
                    inv.GetAcceptedItems(mWorkItemType);
                    foreach (var it in mWorkItemType) {

                        var info = it.GetItemInfo();
                        var currentVolume = 0f;
                        string tag;
                        if (getTag4Item(it.ToString(), out tag)) {
                            MyInventoryItem item;
                            if (mWorkInventoryItem.TryGetValue(it.ToString(), out item)) {
                                currentVolume = (float)item.Amount * info.Volume;
                            }
                            var need = preferredVolume - currentVolume;

                            mLog.persist($"{tag} {currentVolume}/{preferredVolume} for {r.CustomName}");
                            if (need < -0.1) {
                                // move out
                                sortBlock = r;
                                sortInventory = inv;
                                sortItem = item;
                                sortTag = tag;
                                while (sortMachine.MoveNext() && sortMachine.Current) {
                                    yield return true;
                                }
                                //mLog.persist($"Transferring {tag} out of {r.CustomName}");
                            } else if (need > 0.1) {
                                List<InventoryRegister> irList;
                                if (mInventoryRegister.TryGetValue(tag, out irList)) {
                                    foreach (var ir in irList) {
                                        if (inv.TransferItemFrom(ir.Inventory, ir.Item, (MyFixedPoint)(need / info.Volume))) {
                                            break;
                                        }
                                    }
                                }
                            } else {
                                continue;
                            }
                        }
                    }
                    mWorkItemType.Clear();
                    mWorkInventoryItem.Clear();

                    if (mRefine.Count > 0) {
                        MyInventoryItem? item = new MyInventoryItem();
                        var count = inv.ItemCount;
                        while (item.HasValue && count > 0) {
                            count--;
                            item = inv.GetItemAt(0);
                            if (item.HasValue) {
                                string tag;
                                var gotTag = getTag4Item(item.Value, out tag);
                                if (gotTag && tag == mRefine[0].Ore) {
                                    //transfer -= (float)item.Value.Amount * item.Value.Type.GetItemInfo().Volume;
                                    // todo ensure volume
                                    break;
                                } else {
                                    if (!inv.TransferItemFrom(inv, 0, inv.ItemCount, true)) {
                                        break;
                                    }
                                    yield return true;
                                }
                            }
                        }
                    }
                }
                yield return false;
            }
        }
        IEnumerator<bool> stepAssemblerStockMachine;
        IEnumerator<bool> stepAssemblerStock() {
            yield return true;
            while (true) {
                foreach (var a in mAssemblers) {
                    var inv = a.InputInventory;
                    var maxVolume = (double)inv.MaxVolume * 0.9;
                    mLog.persist($"Volume for assembler is {maxVolume}");
                    int pos = -1;
                    foreach (var res in mResource.Values) {
                        double volumeFactor = res.IngotType.GetItemInfo().Volume;
                        //mLog.persist($"{res.Ingot}.volumeFactor={volumeFactor}");
                        var item = inv.GetItemAt(++pos);
                        string tag;

                        double missingMass = 0;
                        if (item.HasValue) {

                            if (getTag4Item(item.Value, out tag)) {
                                if (tag == res.Ingot) {
                                    // todo make item info part of ResourceInfo
                                    var currentMass = (double)item.Value.Amount;
                                    double currentVolume = currentMass * volumeFactor;
                                    // .5 = 1 * .5

                                    var currentRatio = currentVolume / maxVolume;
                                    // .05 = .5 / 10

                                    var missingRatio = res.dRatio - currentRatio;
                                    // .15 = .2 - .05

                                    if (missingRatio > 0) {
                                        var missingVolume = missingRatio * maxVolume;
                                        // 1.5 = .15 * 10
                                        missingMass = missingVolume / volumeFactor;
                                        mLog.persist($"{tag} currentMass={currentMass}, currentVolume={currentVolume}, currentRatio={currentRatio}, missingRatio={missingRatio}, missingVolume={missingVolume}, missingMass={missingMass} ");
                                    }

                                    //mLog.persist($"{tag} is {v}(raw:{item.Value.Amount.RawValue}) over total is {r} target {res.dRatio} move {v2move}");
                                } else {
                                    // todo sort out
                                    mLog.persist($"remove {tag} pos {pos} want {res.Ingot}");
                                }
                            }
                            //v2move = -1;
                        } else {
                            var missingVolume = res.dRatio * maxVolume;
                            missingMass = missingVolume / volumeFactor;
                        }
                        if (missingMass > 0.1) {
                            List<InventoryRegister> list;
                            if (mInventoryRegister.TryGetValue(res.Ingot, out list)) {
                                if (list.Count > 0) {
                                    double iv = list[0].Item.Type.GetItemInfo().Volume;
                                    var mfp = (MyFixedPoint)missingMass;
                                    foreach (var ir in list) {
                                        if (ir.Item.Amount > mfp) {
                                            //mLog.persist($"{res.Ingot} {ir.Item.Amount} > {a2move}");
                                            if (ir.Inventory.TransferItemTo(inv, ir.Item, mfp)) {
                                                break;
                                            }
                                        } else {
                                            //mLog.persist($"{res.Ingot} {ir.Item.Amount} < {a2move}");
                                            if (ir.Inventory.TransferItemTo(inv, ir.Item)) {
                                                mfp -= ir.Item.Amount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                yield return false;
            }
        }

        IEnumerator<bool> stepCargoCountMachine;
        IEnumerator<bool> stepCargoCount() {
            yield return true;
            while (true) {
                foreach (var list in mInventoryRegister.Values) {
                    list.Clear();
                    yield return true;
                }
                int count = mCargo.Count;
                int last = count - 1;
                for (int i = 0; i < count; i++) {
                    stepCountInventory = mCargo[i].GetInventory();
                    stepCountRegister = true;
                    yield return true;
                    while (stepCountMachine.MoveNext() && stepCountMachine.Current) {
                        yield return true;
                    }
                    if (i < last) {
                        yield return true;
                    }
                }
                yield return false;
            }
        }

        void calcResourceRatio(string ingot, string ore, long div, ref long ratio, ref string make) {
            MyFixedPoint value;
            bool assign;
            long r = -1;
            string tag;
            getTag4Item(ingot, out tag);
            if (mInventory.TryGetValue(ingot, out value)) {
                r = value.RawValue / div;                
                assign = r < ratio;
            } else {
                mLog.persist($"fail get inv ingot {ingot}");
                assign = true;
                r = 0;
            }

            if (assign) {
                if (mInventory.TryGetValue(ore, out value)) {
                    if (value > 0) {
                        make = tag;
                        if (r > -1) {
                            ratio = r;
                        }
                    }
                } else {
                    mLog.persist($"fail get inv ore {ore}");
                }
            }
        }
        readonly List<ResourceInfo> mRefine = new List<ResourceInfo>();
        //string makeResource;
        //string makeResourceTag;
        IEnumerator<bool> calcResourceRatiosMachine;
        IEnumerator<bool> calcResourceRatios() {
            yield return true;
            MyFixedPoint ingotLevel;
            while (true) {
                mRefine.Clear();
                foreach (var kvp in mResource) {
                    var res = kvp.Value;
                    var key = res.IngotType.ToString();

                    res.TempRatio = 0;
                    if (mInventory.TryGetValue(key, out ingotLevel)) {
                        res.TempRatio = ingotLevel.RawValue / res.lRatio;
                        mLog.persist($"make ratio {res.TempRatio} for {key}");
                        
                    } else {
                        mLog.persist($"failed to get inventory for {key}");
                    }
                    mRefine.Add(res);
                    yield return true;
                }
                mRefine.Sort((a, b) => a.TempRatio > b.TempRatio ? 1 : -1);
                yield return false;

                /*long ratio = long.MaxValue;
                makeResource = null;
                calcResourceRatio("MyObjectBuilder_Ingot/Iron", "MyObjectBuilder_Ore/Iron", iron.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Cobalt", "MyObjectBuilder_Ore/Cobalt", cobalt.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Nickel", "MyObjectBuilder_Ore/Nickel", nickel.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Silicon", "MyObjectBuilder_Ore/Silicon", silicon.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Stone", "MyObjectBuilder_Ore/Stone", stone.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Silver", "MyObjectBuilder_Ore/Silver", silver.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Gold", "MyObjectBuilder_Ore/Gold", gold.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Magnesium", "MyObjectBuilder_Ore/Magnesium", magnesium.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Platinum", "MyObjectBuilder_Ore/Platinum", platinum.lRatio, ref ratio, ref makeResource);
                yield return true;
                calcResourceRatio("MyObjectBuilder_Ingot/Uranium", "MyObjectBuilder_Ore/Uranium", uranium.lRatio, ref ratio, ref makeResource);


                if (makeResource == null) {
                    makeResourceTag = null;
                    mLog.persist("Break out the drill it's mining time.");
                } else {
                    makeResourceTag = mResource[makeResource].Ore;
                    mLog.persist($"want to refine {makeResourceTag}");
                }
                yield return false;*/
            }

        }
        int mProcessStep;
        string mProcessState;
        string mWorstProcess;
        IEnumerator<bool> processMachine;
        IEnumerator<bool> process() {


            while (true) {
                var start = DateTime.Now;

                mProcessStep = 0;
                mProcessState = "stepRefinerySortMachine";
                while (stepRefinerySortMachine.MoveNext() && stepRefinerySortMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepAssemblerSortMachine";
                while (stepAssemblerSortMachine.MoveNext() && stepAssemblerSortMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepCargoSortMachine";
                while (stepCargoSortMachine.MoveNext() && stepCargoSortMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepRefineryCountMachine";
                while (stepRefineryCountMachine.MoveNext() && stepRefineryCountMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepAssemblerCountMachine";
                while (stepAssemblerCountMachine.MoveNext() && stepAssemblerCountMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepCargoCountMachine";
                while (stepCargoCountMachine.MoveNext() && stepCargoCountMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "calcResourceRatiosMachine";
                while (calcResourceRatiosMachine.MoveNext() && calcResourceRatiosMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepRefineMachine";
                while (stepRefineMachine.MoveNext() && stepRefineMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "stepAssemblerStockMachine";
                while (stepAssemblerStockMachine.MoveNext() && stepAssemblerStockMachine.Current) {
                    mProcessStep++;
                    yield return true;
                }
                yield return true;

                mProcessStep = 0;
                mProcessState = "logInventory";
                if (mDisplay != null) {
                    foreach (var p in mInventory) {
                        var key = p.Key;
                        if (key.StartsWith("MyObjectBuilder_")) {
                            key = key.Substring(16);
                        }
                        long amount = (long)p.Value;
                        mBuilder.AppendFormat("{0,-40}", key);
                        mBuilder.AppendFormat("{0,10}", amount);
                        mBuilder.AppendLine();
                        yield return true;
                    }
                    mDisplay.WriteText(mBuilder.ToString());
                    mBuilder.Clear();
                }
                mInventory.Clear();
                mLog.persist($"complete {(DateTime.Now - start).TotalSeconds:f2}, {mWorstProcess} {maxEver}.");
                mManager.mLog.onUpdate();
                maxEver = 0;
                yield return false;
            }
        }
        double maxEver;
        int runcount = 0;
        readonly int mProcStepsPerUpdate = 1;
        string working = "\\|/-";
        int work = 0;
        public void Main(string arg, UpdateType update) {
            var ms = Runtime.LastRunTimeMs;
            mManager.mLag.Update(ms);
            runcount++;
            
            if (runcount > 12) {
                /*if (mManager.mLag.Current < 0.9) {
                    if (mProcStepsPerUpdate < 10) {
                        mProcStepsPerUpdate++;
                    }
                } else if (mManager.mLag.Current > 1.0) {
                    mProcStepsPerUpdate = 1;
                } else if (mProcStepsPerUpdate > 1) {
                    mProcStepsPerUpdate--;
                }*/
                if (ms > maxEver) {
                    mWorstProcess = mProcessState;
                    maxEver = ms;
                }
            }
            if ((update & (UpdateType.Update10)) != 0) {
                Echo($"{working[work]} {mProcessState} {mProcessStep}");
                work++;
                if (work == working.Length)
                    work = 0;
                //mManager.Update(arg, update);
                
                try {

                    //mLog.log(mManager.mLag.Value, " - ", maxEver, " - ", mProcessState, " ", mProcessStep);
                    for (int i = 0; i < mProcStepsPerUpdate; i++) {
                    //for (int i = 0; i < 100; i++) {
                        processMachine.MoveNext();
                        if (!processMachine.Current) {
                            break;
                        }
                    }
                    
                } catch (Exception ex) {
                    Echo(ex.ToString());
                    mLog.persist(ex.ToString());
                }
            }
        }
    }
}
