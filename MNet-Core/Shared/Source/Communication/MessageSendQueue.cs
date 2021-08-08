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

            public Channel Add(ArraySegment<byte> message, byte channel)
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

            List<NetworkStream> buffers;

            public bool Empty => buffers.Count == 0 && stream.Position == 0;

            NetworkStream stream;

            public void Add(ArraySegment<byte> message)
            {
                if (message.Count > MTU) throw new Exception($"Message Too Big for {MTU} MTU");

                if (stream.Position + message.Count > MTU)
                {
                    buffers.Add(stream);
                    stream = NetworkStream.Pool.Any;
                }

                stream.Insert(message);
            }

            public List<NetworkStream> Read()
            {
                if (stream.Position > 0)
                {
                    buffers.Add(stream);
                    stream = NetworkStream.Pool.Any;
                }

                return buffers;
            }

            public void Clear()
            {
                buffers.Clear();
            }

            public Channel(byte index, int mtu)
            {
                this.Index = index;
                this.MTU = mtu;

                buffers = new List<NetworkStream>();

                stream = NetworkStream.Pool.Any;
            }
        }

        public HashSet<(Delivery delivery, Channel channel)> Dirty;

        public void Add(ArraySegment<byte> message, DeliveryMode mode, byte channel)
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
            foreach (var entry in Dirty)
            {
                var streams = entry.channel.Read();

                for (int i = 0; i < streams.Count; i++)
                {
                    var segment = streams[i].Segment();

                    var packet = new MessageQueuePacket(segment, entry.delivery.Mode, entry.channel.Index);

                    yield return packet;

                    streams[i].Recycle();
                }

                entry.channel.Clear();
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
        public readonly ArraySegment<byte> segment;
        public readonly DeliveryMode delivery;
        public readonly byte channel;

        public MessageQueuePacket(ArraySegment<byte> segment, DeliveryMode delivery, byte channel)
        {
            this.segment = segment;
            this.delivery = delivery;
            this.channel = channel;
        }
    }
}