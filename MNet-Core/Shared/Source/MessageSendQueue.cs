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

            NetworkWriter writer;

            public void Add(NetworkMessage message)
            {
                Collection.Add(message);
            }

            public IEnumerable<byte[]> Serialize(int mtu)
            {
                int position = 0;

                for (int i = 0; i < Count; i++)
                {
                    writer.Write(Collection[i]);

                    if (writer.Size > mtu)
                    {
                        if (writer.Size - position > mtu) //Check the size of the current Network Message
                            throw new Exception($"Network Message with Payload of '{Collection[i].Type}' is Too Big to Fit in an MTU of {mtu}");

                        var segment = writer.ToArray(position); //Read all the way to the previous Network Message and make a segment out of that

                        writer.Shift(position); //Shift the bytes of the current Network Message to the start of the stream

                        yield return segment;
                    }

                    position = writer.Position;
                }

                if (writer.Size > 0)
                {
                    var segment = writer.ToArray();

                    yield return segment;
                }

                Collection.Clear();

                writer.Clear();
            }

            public Delivery(DeliveryMode mode)
            {
                this.Mode = mode;

                Collection = new List<NetworkMessage>();

                writer = new NetworkWriter(1024);
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
        public void Resolve(SimpleResolveDelegate method, int mtu)
        {
            for (int i = 0; i < Deliveries.Count; i++)
            {
                if (Deliveries[i].Count == 0) continue;

                foreach (var binary in Deliveries[i].Serialize(mtu))
                    method(binary, Deliveries[i].Mode);
            }
        }

        public delegate bool ReturnResolveDelegate(byte[] raw, DeliveryMode mode);
        public void Resolve(ReturnResolveDelegate method, int mtu)
        {
            for (int i = 0; i < Deliveries.Count; i++)
            {
                if (Deliveries[i].Count == 0) continue;

                foreach (var binary in Deliveries[i].Serialize(mtu))
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