using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
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
        const long uranium      = 1000000;

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

        IMyTextPanel mLCD;
        IMyTextPanel mDisplay;
        IMyTextPanel mHelp;

        //readonly IMyBroadcastListener listener;

        readonly List<IMyCargoContainer> mCargo = new List<IMyCargoContainer>();
        //readonly List<IMyProductionBlock> mProduction = new List<IMyProductionBlock>();
        readonly List<IMyRefinery> mRefineries = new List<IMyRefinery>();
        readonly List<IMyAssembler> mAssemblers = new List<IMyAssembler>();

        readonly List<IMyTerminalBlock> mWorkList = new List<IMyTerminalBlock>();
        //readonly Inventory inventory = new Inventory();
        //readonly Dictionary<long, ForeignOrder> foreignOrders = new Dictionary<long, ForeignOrder>();
        //readonly Dictionary<string, long> mInventoryAvailable = new Dictionary<string, long>();
        //readonly Dictionary<string, long> mInventoryRequired = new Dictionary<string, long>();

        //readonly Dictionary<long, InventoryOrder> unconfirmedOutgoing = new Dictionary<long, InventoryOrder>();
        //readonly Dictionary<string, List<InventoryOrder>> confirmedOutgoing = new Dictionary<string, List<InventoryOrder>>();
        
        //readonly List<InventoryOrder> unconfirmedIncoming = new List<InventoryOrder>();        
        //readonly Dictionary<string, List<InventoryOrder>> confirmedIncoming = new Dictionary<string, List<InventoryOrder>>();
        readonly SortedDictionary<string, MyFixedPoint> mInventory = new SortedDictionary<string, MyFixedPoint>();
        //const int broadcastInterval = 10;

        readonly SortedDictionary<string, string> mToTag = new SortedDictionary<string, string>();

        readonly Dictionary<string, List<InventoryRegister>> mInventoryRegister = new Dictionary<string, List<InventoryRegister>>();

        // Steps step;
        // int supIndex = 0;
        // int index = 0;
        // int subIndex = 0;
        
        // double lastBroadcast = 0;
        // double lastShipment = 0;

        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            mManager = new ModuleManager(this, "Inventory Manager", "inventoryLogConsole");
            mLog = mManager.mLog;
            mLog.PersistentMax = 25;
            //listener = IGC.RegisterBroadcastListener(tagInventory);
            //listener.SetMessageCallback(tagInventory);
            mManager.Initialize();
            init();

            processMachine = process();
            preSortMachine = preSort();
            sortMachine = sort();
            stepProductionSortMachine = stepProductionSort();
            stepRefinerySortMachine = stepRefinerySort();
            stepAssemblerSortMachine = stepAssemblerSort();
            stepCargoSortContainerMachine = stepCargoSortContainer();
            stepCargoSortMachine = stepCargoSort();
            stepCountMachine = stepCount();
            stepRefineryCountMachine = stepRefineryCount();
            stepAssemblerCountMachine = stepAssemblerCount();
            stepCargoCountMachine = stepCargoCount();
            stepRefineMachine = stepRefine();
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
            mLCD = null;
            mManager.getByTag("inventoryConsole", ref mLCD);
            mManager.getByTag("inventoryDisplay", ref mDisplay);
            mManager.getByTag("inventoryHelp", ref mHelp);

            initTags();

            if (mHelp != null) {
                showHelp();
            }

            mCargo.Clear();
            mManager.getByTag(tagInventory, mCargo);
            mLog.persist($"Found {mCargo.Count} inventory container.");

            mManager.getByType(mRefineries);
            foreach(var r in mRefineries) {
                r.Enabled = true;
                r.UseConveyorSystem = false;
            }

            mManager.getByType(mAssemblers);
            foreach(var a in mAssemblers) {
                a.Enabled = true;
                a.UseConveyorSystem = false;
                a.Mode = MyAssemblerMode.Assembly;
                a.CooperativeMode = false;
                a.Repeating = false;
                a.ClearQueue();
            }

        }
        void initTags() {
            mToTag["MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm"] = "nato556";
            mToTag["MyObjectBuilder_AmmoMagazine/NATO_25x184mm"] = "nato25";
            mToTag["MyObjectBuilder_AmmoMagazine/Missile200mm"] = "missile";
            mToTag["MyObjectBuilder_Component/SteelPlate"] = "plate";
            mToTag["MyObjectBuilder_Component/SolarCell"] = "solar";
            mToTag["MyObjectBuilder_Component/Girder"] = "girder";
            mToTag["MyObjectBuilder_Component/SmallTube"] = "tube";
            mToTag["MyObjectBuilder_Component/Detector"] = "detector";
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
            while (true) {
                mWorkList.Clear();
                mManager.getByTag(sortTag, mWorkList);
                if (mWorkList.Count == 0 && !mManager.hasTag(sortBlock, tagAnything)) {
                    mManager.getByTag(tagAnything, mWorkList);
                }
                if (mWorkList.Count != 0) {
                    itemInfo = sortItem.Type.GetItemInfo();
                    foreach (var c in mWorkList) {
                        if (c.EntityId != sortBlock.EntityId) {
                            var inv = c.GetInventory();
                            var max = (float)inv.MaxVolume;
                            var cur = (float)inv.CurrentVolume;
                            var free = max - cur;
                            var volume = (float)sortItem.Amount * itemInfo.Volume;
                            if (free > volume) {
                                if (sortInventory.TransferItemTo(inv, sortItem)) {
                                    yield return false;
                                }
                            } else {
                                var amt = sortItem.Amount;
                                // todo use aItem.Type.GetItemInfo().Volume ??
                                if (itemInfo.UsesFractions) {
                                    amt.RawValue = (long)(free / itemInfo.Volume);
                                } else {
                                    amt.RawValue = (int)(free / itemInfo.Volume);
                                }
                                amt.RawValue *= 1000000;
                                if (sortInventory.TransferItemTo(inv, sortItem, amt)) {
                                    yield return false;
                                }
                            }
                        }
                        yield return true;
                    }
                }
                yield return false;
            }
        }
        IEnumerator<bool> preSortMachine;
        IMyTerminalBlock preSortBlock;
        IMyInventory preSortInventory;
        MyInventoryItem preSortItem;
        IEnumerator<bool> preSort(/*IMyTerminalBlock aSource, IMyInventory aSourceInventory, MyInventoryItem aItem*/) {
            string tag;
            while (true) {
                if (getTag4Item(preSortItem, out tag)) {
                    if (!mManager.hasTag(preSortBlock, tag)) {
                        sortBlock = preSortBlock;
                        sortInventory = preSortInventory;
                        sortItem = preSortItem;
                        sortTag = tag;
                        while (sortMachine.MoveNext() && sortMachine.Current) {
                            mLog.log("preSort");
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
            while (true) {
                var inv = stepProductionSortBlock.OutputInventory;
                for (int i = 0; true; i++) {
                    var item = inv.GetItemAt(i);
                    if (item.HasValue) {
                        preSortBlock = stepProductionSortBlock;
                        preSortInventory = inv;
                        preSortItem = item.Value;
                        while (preSortMachine.MoveNext() && sortMachine.Current) {
                            mLog.log("stepProductionSort - yielding true");
                            yield return true;
                        }
                        yield return true;
                    } else {
                        mLog.log("stepProductionSort - break");
                        break;
                    }
                }
                mLog.log("stepProductionSort - yielding false");
                yield return false;
            }
        }
        readonly IEnumerator<bool> stepAssemblerSortMachine;
        IEnumerator<bool> stepAssemblerSort() {
            while (true) {
                foreach (var a in mAssemblers) {
                    stepProductionSortBlock = a;
                    while (stepProductionSortMachine.MoveNext() && sortMachine.Current) {
                        mLog.log("stepAssemblerSort");
                        yield return true;
                    }
                    yield return true;
                }
                yield return false;
            }
        }
        readonly IEnumerator<bool> stepRefinerySortMachine;
        IEnumerator<bool> stepRefinerySort() {
            while (true) {
                foreach (var r in mRefineries) {
                    stepProductionSortBlock = r;
                    while (stepProductionSortMachine.MoveNext() && stepProductionSortMachine.Current) {
                        mLog.log("stepRefinerySort - yielding true");
                        yield return true;
                    }
                }
                mLog.log("stepRefinerySort - yielding false");
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
            while (true) {
                var inv = stepCargoSortBlock.GetInventory();
                if (inv != null) {
                    for (int i = 0; true; i++) {
                        var item = inv.GetItemAt(i);
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
            while (true) {
                foreach (var c in mCargo) {
                    stepCargoSortBlock = c;
                    while (stepCargoSortContainerMachine.MoveNext() && stepCargoSortContainerMachine.Current) {
                        mLog.log("stepCargoSort");
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
        
        void logInventory() {
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
        readonly IEnumerator<bool> stepCountMachine;
        IMyInventory stepCountInventory;
        bool stepCountRegister;
        IEnumerator<bool> stepCount(/*IMyInventory aInventory, bool register*/) {
            while (true) {
                for (int i = 0; true; i++) {
                    var item = stepCountInventory.GetItemAt(i);
                    if (item.HasValue) {
                        if (stepCountRegister) {
                            registerInventory(stepCountInventory, item.Value);
                        }
                        stepCount(item.Value);
                        yield return true;
                    } else {
                        break;
                    }
                }
                yield return false;
            }
        }
        IEnumerator<bool> stepRefineryCountMachine;
        IEnumerator<bool> stepRefineryCount() {
            while (true) {
                foreach (var r in mRefineries) {
                    stepCountInventory = r.InputInventory;
                    stepCountRegister = false;
                    while (stepCountMachine.MoveNext() && stepCountMachine.Current) {
                        mLog.log("stepRefineryCount");
                        yield return true;
                    }
                    yield return true;
                }
                yield return false;
            }
        }
        IEnumerator<bool> stepAssemblerCountMachine;
        IEnumerator<bool> stepAssemblerCount() {
            while (true) {
                foreach (var a in mAssemblers) {
                    stepCountInventory = a.InputInventory;
                    stepCountRegister = false;
                    while (stepCountMachine.MoveNext() && stepCountMachine.Current) {
                        mLog.log("stepRefineryCount");
                        yield return true;
                    }
                    yield return true;
                }
                yield return false;
            }
        }
        void clearLists() {
            foreach (var list in mInventoryRegister.Values) {
                list.Clear();
            }
        }
        IEnumerator<bool> stepRefineMachine;
        IEnumerator<bool> stepRefine(/*IMyRefinery aRefinery*/) {
            while (true) {
                foreach (var r in mRefineries) {
                    var inv = r.InputInventory;
                    var item = inv.GetItemAt(0);
                    if (item.HasValue) {
                        string tag;
                        var gotTag = getTag4Item(item.Value, out tag);
                        if (gotTag && tag == makeResourceTag) {
                            // todo ensure volume
                        } else {
                            sortBlock = r;
                            sortInventory = inv;
                            sortItem = item.Value;
                            sortTag = tag;
                            while (sortMachine.MoveNext() && sortMachine.Current) {
                                yield return true;
                            }
                        }
                    } else {
                        List<InventoryRegister> list;

                        if (mInventoryRegister.TryGetValue(makeResourceTag, out list)) {
                            var transfer = 7f; // volume to transfer m^3

                            foreach (var ir in list) {
                                var info = ir.Item.Type.GetItemInfo();
                                var kgvol = info.Volume;
                                var kgamt = (float)ir.Item.Amount;
                                var mcavail = kgamt * kgvol;
                                if (mcavail > transfer) {
                                    MyFixedPoint mfp = (MyFixedPoint)(transfer / info.Volume);
                                    if (ir.Inventory.TransferItemTo(inv, ir.Item, mfp)) {
                                        break;
                                    } else {
                                        mLog.persist("full transfer failure");
                                    }
                                } else {
                                    MyFixedPoint mfp = (MyFixedPoint)(mcavail / info.Volume);
                                    if (ir.Inventory.TransferItemTo(inv, ir.Item, mfp)) {
                                        transfer -= mcavail;
                                    } else {
                                        mLog.persist("partial transfer failure");
                                    }
                                }
                                yield return true;
                            }

                        } else {
                            mLog.persist("Could not get IR List");
                        }
                    }
                    yield return true;
                }
                yield return false;
            }
        }

        IEnumerator<bool> stepCargoCountMachine;
        IEnumerator<bool> stepCargoCount() {
            while (true) {
                clearLists();
                foreach (var c in mCargo) {
                    stepCountInventory = c.GetInventory();
                    stepCountRegister = true;
                    while (stepCountMachine.MoveNext() && stepCountMachine.Current) {
                        mLog.log("stepCargoCount");
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
            if (mInventory.TryGetValue(ingot, out value)) {
                r = value.RawValue / div;
                string tag;
                getTag4Item(ingot, out tag);
                assign = r < ratio;
            } else {
                //mLog.persist($"failed to get inventory for {ingot}");
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
                } else {
                    //mLog.persist($"failed to get inventory for {ore}");
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
                mLog.persist("Break out the drill it's mining time.");
            } else {
                makeResourceTag = mToTag[makeResource];
            }

            
        }
        IEnumerator<bool> processMachine;
        IEnumerator<bool> process() {
            while (true) {

                mLog.log("Process step to stepRefinerySortMachine");
                while (stepRefinerySortMachine.MoveNext() && stepRefinerySortMachine.Current) {
                    mLog.log("Process step to stepRefinerySortMachine yielding");
                    yield return true;
                }

                mLog.log("Process step to stepAssemblerSortMachine");
                while (stepAssemblerSortMachine.MoveNext() && stepAssemblerSortMachine.Current) {
                    mLog.log("Process step to stepAssemblerSortMachine yielding");
                    yield return true;
                }

                mLog.log("Process step to stepCargoSortMachine");
                while (stepCargoSortMachine.MoveNext() && stepCargoSortMachine.Current) {
                    mLog.log("Process step to stepCargoSortMachine yielding");
                    yield return true;
                }

                mLog.log("Process step to stepRefineryCountMachine");
                while (stepRefineryCountMachine.MoveNext() && stepRefineryCountMachine.Current) yield return true;

                mLog.log("Process step to stepAssemblerCountMachine");
                while (stepAssemblerCountMachine.MoveNext() && stepAssemblerCountMachine.Current) yield return true;

                mLog.log("Process step to stepCargoCountMachine");
                while (stepCargoCountMachine.MoveNext() && stepCargoCountMachine.Current) yield return true;

                mLog.log("Process step to calcResourceRatios");
                calcResourceRatios();
                yield return true;

                mLog.log("Process step to stepRefineMachine");
                while (stepRefineMachine.MoveNext() && stepRefineMachine.Current) yield return true;

                mLog.log("Process step to logInventory");
                logInventory();
                yield return true;
            }
        }
        public void Main(string arg, UpdateType update) {
            if ((update & (UpdateType.Update10)) != 0) {
                mLog.log("Main");
                try {
                    processMachine.MoveNext();
                } catch (Exception ex) {
                    mLog.persist(ex.ToString());
                }
                mManager.Update(arg, update);
            }
        }
    }
}
