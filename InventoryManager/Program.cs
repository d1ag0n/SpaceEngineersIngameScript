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

        readonly Logger g;
        readonly GTS mGTS;

        IMyTextPanel mLCD;

        readonly IMyBroadcastListener listener;

        readonly List<IMyEntity> mCargo = new List<IMyEntity>();
        readonly List<IMyProductionBlock> mProduction = new List<IMyProductionBlock>();
        readonly List<IMyEntity> mWorkList = new List<IMyEntity>();
        readonly Inventory inventory = new Inventory();
        readonly Dictionary<long, ForeignOrder> foreignOrders = new Dictionary<long, ForeignOrder>();
        readonly Dictionary<string, long> mInventoryAvailable = new Dictionary<string, long>();
        readonly Dictionary<string, long> mInventoryRequired = new Dictionary<string, long>();

        readonly Dictionary<long, InventoryOrder> unconfirmedOutgoing = new Dictionary<long, InventoryOrder>();
        readonly Dictionary<string, List<InventoryOrder>> confirmedOutgoing = new Dictionary<string, List<InventoryOrder>>();
        
        readonly List<InventoryOrder> unconfirmedIncoming = new List<InventoryOrder>();        
        readonly Dictionary<string, List<InventoryOrder>> confirmedIncoming = new Dictionary<string, List<InventoryOrder>>();

        const int broadcastInterval = 10;
        Steps step;
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
        void init() {
            step = 0;
            index =
            subIndex = 0;
            mLCD = null;
            mGTS.getByTag("inventoryConsole", ref mLCD);
            
            
            mGTS.initListByTag(tagInventory, mCargo);

            mProduction.Clear();
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
                g.log(aItem.Amount.RawValue);
                g.log("Tag not found for ", aItem.Type);
            }
            return result;
        }
        
        void sort(IMyEntity aSourceCargo, IMyInventory aSourceInventory, MyInventoryItem aItem, string aTag) {
            
            g.log("sorting #", aTag);
            
            mGTS.initListByTag(aTag, mWorkList);
            if (mWorkList.Count == 0 && !mGTS.hasTag((IMyTerminalBlock)aSourceCargo, tagAnything)) {
                mGTS.initListByTag(tagAnything, mWorkList);
            }
            MyItemInfo itemInfo;
            if (mWorkList.Count != 0) {
                itemInfo = aItem.Type.GetItemInfo();
                foreach (var c in mWorkList) {
                    if (c.EntityId != aSourceCargo.EntityId) {
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
            cargo = 0,  // sort inventory into correct locations
            production, // sort production output into correct locations
            inventory,  // look at ship/order tags and generate inventory
            shipping,   // review fill foreign orders
            receiving,  // review pending orders
            done
        }
        
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
        bool canOrder(string aTag) {
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
        }
        void flush() {
            var str = g.clear();
            Echo(str);
            if (null != mLCD) mLCD.WriteText(str);
        }
        void receiveMessages() {
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
        }
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
        }
        bool sendOrder(InventoryOrder aOrder) => IGC.SendUnicastMessage(aOrder.Receiver, tagOrder, aOrder.Serialize());
        long getRequired(string aTag) {
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
        }
        long getAvailable(string aTag) {
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
        }
        void stepShipping() {
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
        }
        
        
        void stepReceiving() {
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
        }
        public void Main(string argument, UpdateType aUpdate) {

            
            //g.log("Main");
            
            receiveMessages();
            
            if ((aUpdate & (UpdateType.Terminal)) != 0) {            
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
            
            if ((aUpdate & (UpdateType.Update10)) != 0) {
                //Echo($"index {index} subindex {subIndex}");
                switch (step) {
                    case Steps.cargo:
                        stepCargo();
                        break;
                    case Steps.production:
                        stepProduction();
                        break;
                    case Steps.inventory:
                        stepInventory();
                        break;
                    case Steps.shipping:
                        stepShipping();
                        break;
                    case Steps.receiving:
                        stepReceiving();
                        break;
                }
                if (Steps.done == step) {
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
                    }
                    step = 0;
                    flush();
                }
            }
        }
    }
}
