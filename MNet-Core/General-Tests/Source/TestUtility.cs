using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

using NUnit.Framework;

using System.Runtime.CompilerServices;

public static class TestUtility
{
    public static object[] TupleToArray(ITuple tuple)
    {
        var array = new object[tuple.Length];

        for (int i = 0; i < tuple.Length; i++)
            array[i] = tuple[i];

        return array;
    }

    public static void Compare(ITuple original, ITuple copy) => Compare(TupleToArray(original), TupleToArray(copy));

    public static void Compare<T>(IEnumerable<T> original, IEnumerable<T> copy) => Compare(original.ToArray(), copy.ToArray());

    public static void Compare<T>(T[] original, T[] copy)
    {
        if (original.Length != copy.Length) Assert.Fail("Mismatched Length Between Original and Copy Collections");

        for (int i = 0; i < original.Length; i++)
        {
            if (Equals(original[i], copy[i]) == false)
                Assert.Fail($"Mismatched Element at index {i}, {original[i]} != {copy[i]}");
        }
    }
}