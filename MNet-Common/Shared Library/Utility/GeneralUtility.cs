using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using System.Threading.Tasks;

namespace MNet
{
    public static class GeneralUtility
    {
        public static string PrettifyName<T>(T value)
        {
            var text = value.ToString();

            var builder = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                var current = text[i];

                if (char.IsUpper(current))
                {
                    if (i + 1 < text.Length && i > 0)
                    {
                        var next = text[i + 1];
                        var previous = text[i - 1];

                        if (char.IsLower(previous))
                            builder.Append(" ");
                    }
                }

                builder.Append(text[i]);
            }

            return builder.ToString();
        }

        public static class Time
        {
            public static class Milliseconds
            {
                public static int FromSeconds(int seconds) => seconds * 1000;

                public static int FromMinutes(int minutes) => FromSeconds(minutes * 60);
            }
        }

        public static class Collection
        {
            public static float Average(IList<float> list)
            {
                var sum = 0f;

                for (int i = 0; i < list.Count; i++)
                    sum += list[i];

                return sum / list.Count;
            }
            public static float Min(IList<float> list)
            {
                var value = float.MaxValue;

                for (int i = 0; i < list.Count; i++)
                    if (list[i] < value)
                        value = list[i];

                return value;
            }
            public static float Max(IList<float> list)
            {
                var value = float.MinValue;

                for (int i = 0; i < list.Count; i++)
                    if (list[i] > value)
                        value = list[i];

                return value;
            }

            public static double Average(IList<double> list)
            {
                var sum = 0d;

                for (int i = 0; i < list.Count; i++)
                    sum += list[i];

                return sum / list.Count;
            }
            public static double Min(IList<double> list)
            {
                var value = double.MaxValue;

                for (int i = 0; i < list.Count; i++)
                    if (list[i] < value)
                        value = list[i];

                return value;
            }
            public static double Max(IList<double> list)
            {
                var value = double.MinValue;

                for (int i = 0; i < list.Count; i++)
                    if (list[i] > value)
                        value = list[i];

                return value;
            }
        }
    }

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

        public static string ToPrettyString<T>(this T value) => GeneralUtility.PrettifyName(value);

        public static async void Forget(this Task task) => await task;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<TFrom, TTo>(this TFrom target, out TTo value)
        where TFrom : struct
        {
            if (target.GetType() == typeof(TTo))
            {
                value = Unsafe.As<TFrom, TTo>(ref target);
                return true;
            }

            value = default;
            return false;
        }

        public static unsafe bool HasFlagFast<T>(this T target, T flag)
            where T : unmanaged, Enum
        {
            var size = sizeof(T);

            switch (size)
            {
                case 1:
                    return (Unsafe.As<T, byte>(ref target) & Unsafe.As<T, byte>(ref flag)) > 0;
                case 2:
                    return (Unsafe.As<T, ushort>(ref target) & Unsafe.As<T, ushort>(ref flag)) > 0;
                case 4:
                    return (Unsafe.As<T, uint>(ref target) & Unsafe.As<T, uint>(ref flag)) > 0;
                case 8:
                    return (Unsafe.As<T, ulong>(ref target) & Unsafe.As<T, ulong>(ref flag)) > 0;
                default:
                    throw new ArgumentException($"Invalid Enum with Size of {size}");
            }
        }
    }
}