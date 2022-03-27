using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace MNet
{
    class DataTypes
    {
        [SetUp]
        public void Setup()
        {

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

            for (byte i = 0; i < flag.Length; i++)
                Assert.AreEqual(flag[i], i % 2 == 0);

            var clone = NetworkSerializer.Clone(flag);
            Assert.AreEqual(flag, clone);
        }
    }
}