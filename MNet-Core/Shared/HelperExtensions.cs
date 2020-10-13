using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    public static class HelperExtensions
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
    }
}