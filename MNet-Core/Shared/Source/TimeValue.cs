using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MNet
{
    [Preserve]
    [Serializable]
    public class TimeValue : INetworkSerializable
    {
        float value = default;
        public float Value => value;

        public float Milliseconds { get; private set; }
        public float Seconds { get; private set; }

        public void Set(double value) => Set((float)value);
        public void Set(float value)
        {
            this.value = value;

            this.Milliseconds = value;
            this.Seconds = value / 1000f;
        }

        public void Clear() => Set(0);

        public void CalculateFrom(DateTime timestamp) => CalculateFrom(timestamp, 0);
        public void CalculateFrom(DateTime timestamp, double offset)
        {
            var difference = (DateTime.UtcNow - timestamp).TotalMilliseconds;

            Set(difference + offset);
        }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);

            if (context.IsReadingBinary) Set(value);
        }

        public TimeValue()
        {

        }
        public TimeValue(float value)
        {
            Set(value);
        }
    }
}