using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using VRage;

namespace IngameScript
{
    class ForeignOrder
    {
        public double LastShipment;
        public ImmutableDictionary<string, long> Orders;
        public readonly Dictionary<string, InventoryOrder> OpenOrders = new Dictionary<string, InventoryOrder>();
    }
    class InventoryOrder
    {
        public readonly uint Id;
        public double Time;
        public long Shipper;
        public long Receiver;
        public string Item;
        public long Volume;
        public bool Confirmed = false;
        public ForeignOrder Order;
        
        static uint _id = 0;

        public InventoryOrder(object aData) {
            var data = (MyTuple<uint, long, string, long>)aData;
            Id = data.Item1;
            Shipper = data.Item2;
            Item = data.Item3;
            Volume = data.Item4;
        }
        public InventoryOrder() {
            Id = ++_id;
        }
        public MyTuple<uint, long, string, long> Serialize() => new MyTuple<uint, long, string, long>(Id, Shipper, Item, Volume);
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine($"Inventory Order #{Id}");
            sb.AppendLine($"Time     {Time}");
            sb.AppendLine($"Shipper  {Shipper}");
            sb.AppendLine($"Receiver {Receiver}");
            sb.AppendLine($"Item     {Item}");
            sb.AppendLine($"Volume   {Volume}");
            return sb.ToString();
        }
    }
    class Inventory
    {
        public readonly Dictionary<string, long> Ordering = new Dictionary<string, long>();
        public readonly Dictionary<string, long> Shipping = new Dictionary<string, long>();

        public void AddForShipping(string aItem, MyFixedPoint aVolume) => addFor(Shipping, aItem, aVolume);
        public void AddForOrdering(string aItem, MyFixedPoint aVolume) => addFor(Ordering, aItem, aVolume);
        public void Clear() {
            Ordering.Clear();
            Shipping.Clear();
        }
        void addFor(Dictionary<string, long> aDictionary, string aTag, MyFixedPoint aVolume) {
            long value;
            if (aDictionary.TryGetValue(aTag, out value)) {
                value += aVolume.RawValue;
            } else {
                value = aVolume.RawValue;
            }
            aDictionary[aTag] = value;
        }
        public MyTuple<double, ImmutableDictionary<string, long>> SerializeOrders(double aTime) => new MyTuple<double, ImmutableDictionary<string, long>>(aTime, Ordering.ToImmutableDictionary());
    }
}
