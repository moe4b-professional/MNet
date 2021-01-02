using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

using System.Reflection;

namespace MNet
{
    public static class GeneralExtensions
    {
        public static TResult[] ToArray<TResult, Tkey, TValue>(this Dictionary<Tkey, TValue> dictionary, Func<TValue, TResult> function)
        {
            var array = new TResult[dictionary.Count];

            var index = 0;

            foreach (var value in dictionary.Values)
            {
                var item = function(value);

                array[index] = item;

                index += 1;
            }

            return array;
        }

        public static List<TResult> ToList<TResult, TValue>(this IReadOnlyCollection<TValue> collection, Func<TValue, TResult> function)
        {
            if (collection == null) return new List<TResult>(0);

            var list = new List<TResult>(collection.Count);

            foreach (var value in collection)
            {
                var item = function(value);

                list.Add(item);
            }

            return list;
        }

        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryRemove(key, out _);
        }

        public static bool TryGetIndex<T>(this IList<T> collection, out int index, Predicate<T> predicate)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]) == false) continue;

                index = i;
                return true;
            }

            index = default;
            return false;
        }
    }
}