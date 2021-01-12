using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNet
{
    static class Statistics
    {
        public static class Players
        {
            public static ushort Count { get; private set; }

            static readonly object SyncLock = new object();

            public static void Add()
            {
                lock (SyncLock) Count += 1;
            }

            public static void Remove()
            {
                lock (SyncLock) Count -= 1;
            }
        }
    }
}