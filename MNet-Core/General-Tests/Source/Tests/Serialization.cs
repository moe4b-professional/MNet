using NUnit.Framework;

using System;
using System.Collections;
using System.Collections.Generic;

using System.Runtime.CompilerServices;

namespace MNet
{
    public class Serialization
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void DeliverySegmentation()
        {
            var original = new List<RpcTargetRequest>();

            var delivery = new MessageSendQueue.Delivery(DeliveryMode.Unreliable, 500);

            for (byte i = 1; i <= 40; i++)
            {
                var entity = new NetworkEntityID(i);
                var behaviour = new NetworkBehaviourID(i);
                var method = new RpcMethodID(i);
                var target = new NetworkClientID(i);

                var payload = RpcTargetRequest.Write(entity, behaviour, method, target, new byte[] { i, i });

                var message = NetworkMessage.Write(ref payload);
                original.Add(payload);

                var binary = NetworkSerializer.Serialize(message);
                delivery.Add(binary);
            }

            var copy = new List<RpcTargetRequest>();

            foreach (var buffer in delivery.Read())
            {
                foreach (var message in NetworkMessage.ReadAll(buffer))
                {
                    copy.Add(message.Read<RpcTargetRequest>());
                }
            }

            Assert.AreEqual(original.Count, copy.Count);

            for (int i = 0; i < original.Count; i++)
                Compare(original[i], copy[i]);

            static bool Compare(RpcTargetRequest a, RpcTargetRequest b)
            {
                Assert.AreEqual(a.Entity, b.Entity);
                Assert.AreEqual(a.Behaviour, b.Behaviour);
                Assert.AreEqual(a.Method, b.Method);
                Assert.AreEqual(a.Target, b.Target);

                Utility.Compare(a.Raw, b.Raw);

                return true;
            }
        }

        [Test]
        public void AttributesCollection()
        {
            var original = new AttributesCollection();

            original.Set(0, "Hello World");
            original.Set(1, DateTime.Now);
            original.Set(2, Guid.NewGuid());
            original.Set(3, 40f);

            var copy = NetworkSerializer.Clone(original);

            void CheckEquality<T>(ushort key)
            {
                original.TryGetValue<T>(key, out var value1);
                copy.TryGetValue<T>(key, out var value2);

                Assert.AreEqual(value1, value2);
            }

            CheckEquality<string>(0);
            CheckEquality<DateTime>(1);
            CheckEquality<Guid>(2);
            CheckEquality<float>(3);
        }

        [Test]
        public void HashSet()
        {
            var original = new HashSet<int>();

            original.Add(42);
            original.Add(24);
            original.Add(120);
            original.Add(420);

            var copy = NetworkSerializer.Clone(original);

            Utility.Compare(original, copy);
        }

        [Test]
        public void NullableTuple()
        {
            var original = new Tuple<DateTime?, Guid?, int?>(DateTime.Now, null, 42);

            var copy = NetworkSerializer.Clone(original);

            Utility.Compare(original, copy);
        }

        [Test]
        public void Nullable()
        {
            NetworkClientID? original = new NetworkClientID(20);

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }

        [Test]
        public void NullClass()
        {
            SampleClass original = null;

            var copy = NetworkSerializer.Clone(original, typeof(SampleClass));

            Assert.IsTrue(original == null);
            Assert.IsTrue(copy == null);
        }
        class SampleClass : INetworkSerializable
        {
            public void Select(ref NetworkSerializationContext context)
            {
                
            }

            public SampleClass()
            {

            }
        }

        [Test]
        public void NullableList()
        {
            var original = new List<int?>() { 42, null, 12, 420, null, 69 };

            var copy = NetworkSerializer.Clone(original);

            Utility.Compare(original, copy);
        }

        [Test]
        public void Tuple()
        {
            var original = ("Hello World", 4, DateTime.Now, Guid.NewGuid());

            var copy = NetworkSerializer.Clone(original);

            Utility.Compare(original, copy);
        }

        [Test]
        public void Enum()
        {
            var original = RemoteAuthority.Any;

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }
    }
}