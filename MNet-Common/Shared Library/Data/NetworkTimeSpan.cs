using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MNet
{
    [NetworkBlittable]
    [StructLayout(LayoutKind.Sequential)]
    public struct NetworkTimeSpan
    {
        long ticks;
        public long Ticks => ticks;

        public float Millisecond => ticks / 1f / TimeSpan.TicksPerMillisecond;
        public float Seconds => ticks / 1f / TimeSpan.TicksPerSecond;

        public NetworkTimeSpan(long ticks)
        {
            this.ticks = ticks;
        }

        /// <summary>
        /// Calculates a NetworkTimeSpawn from a UTC timestamp
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public static NetworkTimeSpan Calculate(DateTime timestamp) => Calculate(timestamp, 0);
        /// <summary>
        /// Calculates a NetworkTimeSpawn from a UTC timestamp and a tick offset
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static NetworkTimeSpan Calculate(DateTime timestamp, long offset)
        {
            var span = DateTime.UtcNow.Ticks - timestamp.Ticks;

            return new NetworkTimeSpan(span + offset);
        }

        public static NetworkTimeSpan operator +(NetworkTimeSpan span, long ticks) => new NetworkTimeSpan(span.ticks + ticks);
        public static NetworkTimeSpan operator -(NetworkTimeSpan span, long ticks) => new NetworkTimeSpan(span.ticks - ticks);

        public static NetworkTimeSpan operator +(NetworkTimeSpan left, TimeSpan right) => new NetworkTimeSpan(left.ticks + right.Ticks);
        public static NetworkTimeSpan operator -(NetworkTimeSpan left, TimeSpan right) => new NetworkTimeSpan(left.ticks - right.Ticks);
    }
}