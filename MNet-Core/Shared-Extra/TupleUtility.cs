using System;

using System.Runtime.CompilerServices;

namespace MNet
{
    public static class TupleUtility
    {
        public static bool CheckType(Type target) => typeof(ITuple).IsAssignableFrom(target);

        public static object[] Extract(object instance)
        {
            var tuple = instance as ITuple;

            var array = new object[tuple.Length];

            for (int i = 0; i < tuple.Length; i++)
                array[i] = tuple[i];

            return array;
        }
    }
}