using System;
using System.Linq;
using System.Text;

using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading.Tasks;

namespace MNet
{
    public class MessageSendQueue
    {
        public Dictionary<DeliveryMode, Delivery> Dictionary { get; protected set; }

        public List<Delivery> Deliveries { get; protected set; }

        public class Delivery
        {
            public DeliveryMode Mode { get; protected set; }

            public List<NetworkMessage> Collection { get; protected set; }

            public int Count => Collection.Count;

            public bool Empty => Collection.Count == 0;

            public void Add(NetworkMessage message)
            {
                Collection.Add(message);
            }

            public byte[] Serialize()
            {
                var array = Collection.ToArray();

                var binary = NetworkSerializer.Serialize(array);

                Collection.Clear();

                return binary;
            }

            public Delivery(DeliveryMode mode)
            {
                this.Mode = mode;

                Collection = new List<NetworkMessage>();
            }
        }

        public void Add(NetworkMessage message, DeliveryMode mode)
        {
            if (Dictionary.TryGetValue(mode, out var delivery) == false)
            {
                delivery = new Delivery(mode);

                Dictionary.Add(mode, delivery);
                Deliveries.Add(delivery);
            }

            delivery.Add(message);
        }

        public delegate void SimpleResolveDelegate(byte[] raw, DeliveryMode mode);
        public void Resolve(SimpleResolveDelegate method)
        {
            for (int i = 0; i < Deliveries.Count; i++)
            {
                if (Deliveries[i].Count == 0) continue;

                var binary = Deliveries[i].Serialize();

                method(binary, Deliveries[i].Mode);
            }
        }

        public delegate bool ReturnResolveDelegate(byte[] raw, DeliveryMode mode);
        public void Resolve(ReturnResolveDelegate method)
        {
            for (int i = 0; i < Deliveries.Count; i++)
            {
                if (Deliveries[i].Count == 0) continue;

                var binary = Deliveries[i].Serialize();

                method(binary, Deliveries[i].Mode);
            }
        }

        public MessageSendQueue()
        {
            Dictionary = new Dictionary<DeliveryMode, Delivery>();

            Deliveries = new List<Delivery>();
        }
    }
}