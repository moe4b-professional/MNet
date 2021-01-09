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
            var original = new List<string>();

            var delivery = new MessageSendQueue.Delivery(DeliveryMode.Unreliable, 500);

            for (byte i = 1; i <= 40; i++)
            {
                var payload = "XOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXO";

                var message = NetworkMessage.Write(payload);
                original.Add(payload);

                var binary = NetworkSerializer.Serialize(message);
                delivery.Add(binary);
            }

            var copy = new List<string>(original.Count);

            foreach (var buffer in delivery.Read())
            {
                foreach (var message in NetworkMessage.ReadAll(buffer))
                {
                    copy.Add(message.Read<string>());
                }
            }

            Utility.Compare(original, copy);
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

            foreach (var key in original.Keys)
            {
                if (Equals(original[key], copy[key]) == false)
                    Assert.Fail($"Mistmatched Data on Key {key}");
            }
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
        public void ObjectArray()
        {
            var original = new object[]
            {
                "Hello World",
                42,
                Guid.NewGuid(),
                DateTime.Now,
            };

            var copy = NetworkSerializer.Clone(original);

            Utility.Compare(original, copy);
        }

        [Test]
        public void ObjectList()
        {
            var original = new List<object>
            {
                "Hello World",
                20,
                Guid.NewGuid(),
                DateTime.Now,
            };

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