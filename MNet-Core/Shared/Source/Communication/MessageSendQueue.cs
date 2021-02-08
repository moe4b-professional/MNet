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
        public CheckMTUDelegate CheckMTU { get; protected set; }
        public delegate int CheckMTUDelegate(DeliveryMode mode);

        public Dictionary<DeliveryMode, Delivery> Deliveries;

        public class Delivery
        {
            public DeliveryMode Mode { get; protected set; }

            public int MTU { get; protected set; }

            public Dictionary<byte, Channel> Channels;

            public Channel Add(byte[] message, byte channel)
            {
                if (Channels.TryGetValue(channel, out var element) == false)
                {
                    element = new Channel(channel, MTU);

                    Channels.Add(channel, element);
                }

                element.Add(message);

                return element;
            }

            public Delivery(DeliveryMode mode, int mtu)
            {
                this.Mode = mode;
                this.MTU = mtu;

                Channels = new Dictionary<byte, Channel>();
            }
        }

        public class Channel
        {
            public readonly byte Index;

            public readonly int MTU;

            List<byte[]> buffers;

            public bool Empty => buffers.Count == 0 && writer.Size == 0;

            readonly NetworkWriter writer;

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

            public Channel(byte index, int mtu)
            {
                this.Index = index;
                this.MTU = mtu;

                buffers = new List<byte[]>();

                writer = new NetworkWriter(1024);
            }
        }

        public HashSet<(Delivery delivery, Channel channel)> Dirty;

        public void Add(byte[] message, DeliveryMode mode, byte channel)
        {
            if (Deliveries.TryGetValue(mode, out var element) == false)
            {
                var mtu = CheckMTU(mode);

                element = new Delivery(mode, mtu);

                Deliveries.Add(mode, element);
            }

            var target = element.Add(message, channel);

            Dirty.Add((element, target));
        }

        public IEnumerable<MessageQueuePacket> Iterate()
        {
            foreach (var selection in Dirty)
            {
                var buffers = selection.channel.Read();

                for (int i = 0; i < buffers.Count; i++)
                {
                    var packet = new MessageQueuePacket(buffers[i], selection.delivery.Mode, selection.channel.Index);

                    yield return packet;
                }

                selection.channel.Clear();
            }

            Dirty.Clear();
        }

        public void Clear()
        {
            Deliveries.Clear();
            Dirty.Clear();
        }

        public MessageSendQueue(CheckMTUDelegate checkMTU)
        {
            this.CheckMTU = checkMTU;

            Deliveries = new Dictionary<DeliveryMode, Delivery>();
            Dirty = new HashSet<(Delivery, Channel)>();
        }
    }

    [Preserve]
    public struct MessageQueuePacket
    {
        public readonly byte[] raw;
        public readonly DeliveryMode delivery;
        public readonly byte channel;

        public MessageQueuePacket(byte[] raw, DeliveryMode delivery, byte channel)
        {
            this.raw = raw;
            this.delivery = delivery;
            this.channel = channel;
        }
    }
}