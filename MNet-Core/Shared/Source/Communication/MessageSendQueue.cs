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

            List<byte[]> buffers;

            public bool Empty => buffers.Count == 0 && writer.Size == 0;

            public int MTU { get; protected set; }

            NetworkWriter writer;

            public void Add(byte[] message)
            {
                if (message.Length > MTU) throw new Exception($"Message Too Big for {MTU} MTU");

                if (writer.Position + message.Length > MTU)
                {
                    var buffer = writer.ToArray();

                    buffers.Add(buffer);

                    writer.Clear();
                }

                writer.Insert(message);
            }

            public List<byte[]> Read()
            {
                Finialize();

                return buffers;
            }

            void Finialize()
            {
                if (writer.Size == 0) return;

                var buffer = writer.ToArray();

                buffers.Add(buffer);

                writer.Clear();
            }

            public void Clear()
            {
                buffers.Clear();
            }

            public Delivery(DeliveryMode mode, int mtu)
            {
                this.Mode = mode;

                buffers = new List<byte[]>();

                this.MTU = mtu;

                writer = new NetworkWriter(1024);
            }
        }

        public void Add(byte[] message, DeliveryMode mode)
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