using NUnit.Framework;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
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
        public void NullableTuple()
        {
            var original = new Tuple<DateTime?, Guid?, int?>(DateTime.Now, null, 42);

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
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

            var copy = NetworkSerializer.Clone(original);

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
        public void NullString()
        {
            string original = null;

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }

        [Test]
        public void EmptyString()
        {
            string original = "";

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }

        [Test]
        public void IpAddress()
        {
            var original = IPAddress.Loopback;

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }

        [Test]
        public void HashString()
        {
            string original = "%K<@2s7UDp('N_rS,'j`@c8)fX4B^Eqy";

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }

        [Test]
        public void NullableList()
        {
            var original = new List<int?>() { 42, null, 12, 420, null, 69 };

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
        }

        [Test]
        public void Array()
        {
            var original = new int[] { 42, 43, 44, 45 };

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
        }

        [Test]
        public void ArraySegment()
        {
            var array = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

            var original = new ArraySegment<int>(array, 2, 4);

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
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

            TestUtility.Compare(original, copy);
        }

        [Test]
        public void Stack()
        {
            var original = new Stack<int>();

            original.Push(42);
            original.Push(24);
            original.Push(120);
            original.Push(420);

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
        }

        [Test]
        public void Queue()
        {
            var original = new Queue<int>();

            original.Enqueue(42);
            original.Enqueue(24);
            original.Enqueue(120);
            original.Enqueue(420);

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
        }

        [Test]
        public void Dictionary()
        {
            var original = new Dictionary<string, string> { { "Hello", "World" }, { "Rustle", "Mania" }, { "David", "Hayter" } };

            var clone = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, clone);
        }

        [Test]
        public void Tuple()
        {
            var original = ("Hello World", 4, DateTime.Now, Guid.NewGuid());

            var copy = NetworkSerializer.Clone(original);

            TestUtility.Compare(original, copy);
        }

        [Test]
        public void Enum()
        {
            var original = RemoteAuthority.Owner | RemoteAuthority.Master;

            var copy = NetworkSerializer.Clone(original);

            Assert.AreEqual(original, copy);
        }

        [Test]
        public void VarInt()
        {
            for (long i = short.MinValue * 4; i < short.MaxValue * 4; i++)
                Test(i);

            Test(MNet.VarInt.MaxValue);
            Test(MNet.VarInt.MinValue);

            void Test(long value)
            {
                var original = new VarInt(value);

                var copy = NetworkSerializer.Clone(original);

                Assert.AreEqual(original, copy);
            }
        }

        [Test]
        public void UVarInt()
        {
            for (ulong i = ushort.MinValue * 4; i < ushort.MaxValue * 4; i++)
                Test(i);

            Test(MNet.UVarInt.MaxValue);
            Test(MNet.UVarInt.MinValue);

            void Test(ulong value)
            {
                var original = new UVarInt(value);

                var copy = NetworkSerializer.Clone(original);

                Assert.AreEqual(original, copy);
            }
        }

        [Test]
        public void Bool8Flags()
        {
            var flag = new Bool8Flags();
            BoolFlag(flag);
        }

        [Test]
        public void Bool16Flags()
        {
            var flag = new Bool16Flags();
            BoolFlag(flag);
        }

        [Test]
        public void Bool32Flags()
        {
            var flag = new Bool32Flags();
            BoolFlag(flag);
        }

        [Test]
        public void Bool64Flags()
        {
            var flag = new Bool64Flags();
            BoolFlag(flag);
        }

        void BoolFlag<T>(T flag)
            where T : IBoolFlags
        {
            for (byte i = 0; i < flag.Length; i++)
            {
                flag[i] = true;
                flag[i] = false;
                flag[i] = i % 3 == 0;

                flag[i] = i % 2 == 0;
            }

            var clone = NetworkSerializer.Clone(flag);

            Assert.AreEqual(flag, clone);
        }
    }
}