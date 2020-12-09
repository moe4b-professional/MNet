using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MNet
{
	public static class CollectionPrettyPrint
	{
        public static string ToPrettyString(this IEnumerable collection)
        {
            if (collection == null) return "null";

            var builder = new StringBuilder();

            var index = 0;
            builder.Append("[ ");
            foreach (var item in collection)
            {
                if (index > 0) builder.Append(", ");

                builder.Append(ToString(collection, item));

                index++;
            }
            builder.Append(" ]");

            return builder.ToString();
        }

        public static string ToString(IEnumerable collection, object item)
        {
            if (item == null) return "null";

            if (IsValidCollection(item))
                return ToPrettyString(item as IEnumerable);
            else if (IsKeyValue(item))
                return KeyValueToString(item);
            else
                return item.ToString();
        }

        public static bool IsKeyValue(object instance)
        {
            var type = instance.GetType();

            if (type.IsGenericType == false) return false;

            return type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }
        public static string KeyValueToString(object pair)
        {
            var key = pair.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
            var value = pair.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

            return key.GetValue(pair) + ": " + value.GetValue(pair);
        }

        public static bool IsValidCollection(object instance)
        {
            if (instance.GetType() == typeof(string)) return false;

            return instance is IEnumerable;
        }
    }
}