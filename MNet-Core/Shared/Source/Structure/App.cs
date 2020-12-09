using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct AppID : INetworkSerializable
    {
        Guid value;
        public Guid Value { get { return value; } }

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref value);
        }

        public AppID(Guid value)
        {
            this.value = value;
        }

        public static AppID Parse(string text)
        {
            var value = Guid.Parse(text);

            return new AppID(value);
        }
        public static bool TryParse(string text, out AppID id)
        {
            if (Guid.TryParse(text, out var value))
            {
                id = new AppID(value);
                return true;
            }

            id = default;
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(AppID))
            {
                var target = (AppID)obj;

                return target.value == this.value;
            }

            return false;
        }

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString().ToUpper();

        public static bool operator ==(AppID a, AppID b) => a.Equals(b);
        public static bool operator !=(AppID a, AppID b) => !a.Equals(b);

        public static explicit operator AppID(string text) => Parse(text);
    }
}