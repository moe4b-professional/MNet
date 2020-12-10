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

        public CheckMTUDelegate CheckMTU { get; protected set; }
        public delegate int CheckMTUDelegate(DeliveryMode mode);

        public List<Delivery> Deliveries { get; protected set; }

        public class Delivery
        {
            public DeliveryMode Mode { get; protected set; }

            public List<NetworkMessage> Collection { get; protected set; }

            public int Count => Collection.Count;

            public bool Empty => Collection.Count == 0;

            public int MTU { get; protected set; }

            NetworkWriter writer;

            public void Add(NetworkMessage message)
            {
                Collection.Add(message);
            }

            public IEnumerable<byte[]> Serialize()
            {
                int position = 0;

                for (int i = 0; i < Count; i++)
                {
                    writer.Write(Collection[i]);

                    if (writer.Size > MTU)
                    {
                        if (writer.Size - position > MTU) //Check the size of the current Network Message
                            throw new Exception($"Network Message with Payload of '{Collection[i].Type}' is Too Big to Fit in an MTU of {MTU}");

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

                Clear();
            }

            public void Clear()
            {
                Collection.Clear();

                writer.Clear();
            }

            public Delivery(DeliveryMode mode, int mtu)
            {
                this.Mode = mode;

                Collection = new List<NetworkMessage>();

                this.MTU = mtu;

                writer = new NetworkWriter(1024);
            }
        }

        public void Add(NetworkMessage message, DeliveryMode mode)
        {
            if (Dictionary.TryGetValue(mode, out var delivery) == false)
            {
                var mtu = CheckMTU(mode);

                delivery = new Delivery(mode, mtu);

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

                foreach (var binary in Deliveries[i].Serialize())
                    method(binary, Deliveries[i].Mode);
            }
        }

        public delegate bool ReturnResolveDelegate(byte[] raw, DeliveryMode mode);
        public void Resolve(ReturnResolveDelegate method)
        {
            for (int i = 0; i < Deliveries.Count; i++)
            {
                if (Deliveries[i].Count == 0) continue;

                foreach (var binary in Deliveries[i].Serialize())
                    method(binary, Deliveries[i].Mode);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < Deliveries.Count; i++)
                Deliveries[i].Clear();
        }

        public MessageSendQueue(CheckMTUDelegate checkMTU)
        {
            Dictionary = new Dictionary<DeliveryMode, Delivery>();

            this.CheckMTU = checkMTU;

            Deliveries = new List<Delivery>();
        }
    }
}