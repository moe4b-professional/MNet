#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
#endif

using System;
using System.IO;
using System.Linq;
using System.Text;

#if UNITY
using UnityEngine;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections;
using System.Collections.Generic;

using static MNet.XVarInt;

namespace MNet
{
    public static class XVarInt
    {
        public static int CalculateBitCount(long value)
        {
            int factor;

            for (factor = 1; value > 1; factor++)
                value /= 2;

            return factor;
        }
        public static int CalculateBitCount(ulong value)
        {
            int factor;

            for (factor = 1; value > 1; factor++)
                value /= 2;

            return factor;
        }

        public static int CalculateByteCount(int bits) => (bits / 8) + (bits % 8 == 0 ? 0 : 1);

        /// <summary>
        /// Sets the first bit according to the value's sign,
        /// 00000001 for negative numbers, 00000000 for positive numbers
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long WriteSignFlag(long value) => value < 0 ? 1L : 0L;

        /// <summary>
        /// Reads the first bit of the value to determine the sign,
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ReadSignFlag(byte value) => (value & 1) == 1 ? -1 : +1;

        /// <summary>
        /// Reads all binary data according to 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="span"></param>
        /// <param name="bits"></param>
        public static void ReadAllBytes(NetworkReader reader, ref Span<byte> span, out int bits)
        {
            span[0] = reader.TakeByte();

            bits = 8;

            for (int i = 1; i < span.Length; i++)
            {
                if (IsReadBitSet(span[i - 1]))
                    span[i] = reader.TakeByte();
                else
                    break;

                bits += 8;
            }
        }

        public static bool IsReadBitSet(byte value) => IsIndexBitSet(value, 7);
        public static bool IsIndexBitSet(byte value, int index) => (value & (1 << index)) != 0;

        public static class BinaryHelper
        {
            public static string BinaryToString(long value)
            {
                var raw = BitConverter.GetBytes(value);

                return BinaryToString(raw);
            }
            public static string BinaryToString(ulong value)
            {
                var raw = BitConverter.GetBytes(value);

                return BinaryToString(raw);
            }
            public static string BinaryToString(byte[] raw)
            {
                var array = new BitArray(raw);

                var builder = new StringBuilder();

                for (int i = array.Length - 1; i >= 0; i--)
                {
                    builder.Append(array[i] ? 1 : 0);

                    if (i % 8 == 0) builder.Append(" ");
                }

                return builder.ToString();
            }
        }

#if UNITY_EDITOR
        public class BaseDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                var value = property.FindPropertyRelative("value");

                EditorGUI.PropertyField(rect, value, label);
            }
        }
#endif
    }

    [Serializable]
    public struct VarInt : IManualNetworkSerializable
    {
#if UNITY
        [SerializeField]
#endif
        long value;
        public long Value => value;

        public const long MinValue = -36028797018963967;
        public const long MaxValue = 36028797018963967;

        public void Serialize(NetworkWriter writer)
        {
            var value = this.value;

            //The value that actually gets converted to binary
            long exchange = WriteSignFlag(value);

            ///Accounts for bit shif (to skip bits written as sign flag or read flags
            ///Initialized to 1 to account for the sign bit
            var shift = 1;

            value = Math.Abs(value);

            var bits = CalculateBitCount(value);
            var bytes = CalculateByteCount(bits);

            bits += 1; // Accounting for the sign bit
            bits += bytes; //Accounting for all read flags (1 bit at the end of every byte)
            bytes = CalculateByteCount(bits); //Recalculate bytes

            for (int i = 0; i < bits; i++)
            {
                var x = i + shift;

                if ((x + 1) % 8 == 0)
                {
                    exchange |= 1L << x; //Write read flag
                    shift += 1;
                }

                exchange |= (value & (1L << i)) << shift; //Read original bit value and set according to shift
            }

            exchange &= ~(1L << (bytes * 8 - 1)); //Clear the last read flag, if it exists

            Span<byte> span = stackalloc byte[sizeof(long)];

            if (BitConverter.TryWriteBytes(span, exchange) == false)
                throw new Exception($"Failed to Serialize");

            for (int i = 0; i < bytes; i++)
                writer.Insert(span[i]);
        }
        public void Deserialize(NetworkReader reader)
        {
            Span<byte> span = stackalloc byte[sizeof(long)];

            ReadAllBytes(reader, ref span, out var bits);

            var sign = ReadSignFlag(span[0]);

            long exchange = BitConverter.ToInt64(span);

            ///Accounts for bit shif (to skip bits written as sign flag or read flags
            ///Initialized to 1 to account for the sign bit
            var shift = 1;

            value = 0L; //Clear value

            //i starts at 1 to skip the sign bit flag
            for (int i = 1; i < bits; i++)
            {
                if ((i + 1) % 8 == 0)
                {
                    //Skip every read flag
                    shift += 1;
                    continue;
                }

                value |= (exchange & (1L << i)) >> shift; //Read exchange bit value and set according to shift
            }

            value *= sign; //Assign sign
        }

        public override bool Equals(object obj)
        {
            if (obj is VarInt target) return Equals(target);

            return false;
        }
        public bool Equals(VarInt target) => target.value == value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public VarInt(long value)
        {
            this.value = value;
        }

        #region Operators
        public static bool operator ==(VarInt right, VarInt left) => right.Equals(left);
        public static bool operator !=(VarInt right, VarInt left) => !right.Equals(left);

        public static VarInt operator +(VarInt right, VarInt left) => new VarInt(right.value + left.value);
        public static VarInt operator -(VarInt right, VarInt left) => new VarInt(right.value - left.value);

        public static VarInt operator *(VarInt right, VarInt left) => new VarInt(right.value * left.value);
        public static VarInt operator /(VarInt right, VarInt left) => new VarInt(right.value / left.value);

        public static VarInt operator %(VarInt right, VarInt left) => new VarInt(right.value % left.value);

        public static bool operator >(VarInt right, VarInt left) => right.value > left.value;
        public static bool operator <(VarInt right, VarInt left) => right.value < left.value;

        public static bool operator >=(VarInt right, VarInt left) => right.value >= left.value;
        public static bool operator <=(VarInt right, VarInt left) => right.value <= left.value;

        public static VarInt operator ++(VarInt target) => new VarInt(target.value + 1);
        public static VarInt operator --(VarInt target) => new VarInt(target.value - 1);
        #endregion

        #region Conversions
        //From VarInt
        public static implicit operator long(VarInt value) => value.value;
        public static implicit operator int(VarInt value) => (int)value.value;

        //To VarInt
        public static implicit operator VarInt(long value) => new VarInt(value);
        public static implicit operator VarInt(int value) => new VarInt(value);
        public static implicit operator VarInt(short value) => new VarInt(value);
        public static implicit operator VarInt(sbyte value) => new VarInt(value);
        public static implicit operator VarInt(byte value) => new VarInt(value);
        #endregion

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(VarInt))]
        public class Drawer : BaseDrawer { }
#endif
    }

    [Serializable]
    public struct UVarInt : IManualNetworkSerializable
    {
#if UNITY
        [SerializeField]
#endif
        ulong value;
        public ulong Value => value;

        public const ulong MinValue = 0UL;
        public const ulong MaxValue = 72057594037927935UL;

        public void Serialize(NetworkWriter writer)
        {
            //The value that actually gets converted to binary
            ulong exchange = 0UL;

            ///Accounts for bit shif (to skip bits written as sign flag or read flags
            ///Initialized to 0 because we don't have a sign bit
            var shift = 0;

            var bits = CalculateBitCount(value);
            var bytes = CalculateByteCount(bits);

            bits += bytes; //Accounting for all read flags (1 bit at the end of every byte)
            bytes = CalculateByteCount(bits); //Recalculate bytes

            for (int i = 0; i < bits; i++)
            {
                var x = i + shift;

                if ((x + 1) % 8 == 0)
                {
                    exchange |= 1UL << x; //Write read flag
                    shift += 1;
                }

                exchange |= (value & (1UL << i)) << shift; //Read original bit value and set according to shift
            }

            exchange &= ~(1UL << (bytes * 8 - 1)); //Clear the last read flag, if it exists

            Span<byte> span = stackalloc byte[sizeof(long)];

            if (BitConverter.TryWriteBytes(span, exchange) == false)
                throw new Exception($"Failed to Serialize");

            for (int i = 0; i < bytes; i++)
                writer.Insert(span[i]);
        }
        public void Deserialize(NetworkReader reader)
        {
            Span<byte> span = stackalloc byte[sizeof(long)];

            ReadAllBytes(reader, ref span, out var bits);

            ulong exchange = BitConverter.ToUInt64(span);

            ///Accounts for bit shif (to skip bits written as sign flag or read flags
            ///Initialized to 0 because we don't have a sign bit to skip
            var shift = 0;

            value = 0UL; //Clear value

            //i starts at 0 becaused we have no sign bit flag to skip
            for (int i = 0; i < bits; i++)
            {
                if ((i + 1) % 8 == 0)
                {
                    //Skip every read flag
                    shift += 1;
                    continue;
                }

                value |= (exchange & (1UL << i)) >> shift; //Read exchange bit value and set according to shift
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is UVarInt target) return Equals(target);

            return false;
        }
        public bool Equals(UVarInt target) => target.value == value;

        public override int GetHashCode() => value.GetHashCode();

        public override string ToString() => value.ToString();

        public UVarInt(ulong value)
        {
            this.value = value;
        }

        #region Operators
        public static bool operator ==(UVarInt right, UVarInt left) => right.Equals(left);
        public static bool operator !=(UVarInt right, UVarInt left) => !right.Equals(left);

        public static UVarInt operator +(UVarInt right, UVarInt left) => new UVarInt(right.value + left.value);
        public static UVarInt operator -(UVarInt right, UVarInt left) => new UVarInt(right.value - left.value);

        public static UVarInt operator *(UVarInt right, UVarInt left) => new UVarInt(right.value * left.value);
        public static UVarInt operator /(UVarInt right, UVarInt left) => new UVarInt(right.value / left.value);

        public static UVarInt operator %(UVarInt right, UVarInt left) => new UVarInt(right.value % left.value);

        public static bool operator >(UVarInt right, UVarInt left) => right.value > left.value;
        public static bool operator <(UVarInt right, UVarInt left) => right.value < left.value;

        public static bool operator >=(UVarInt right, UVarInt left) => right.value >= left.value;
        public static bool operator <=(UVarInt right, UVarInt left) => right.value <= left.value;

        public static UVarInt operator ++(UVarInt target) => new UVarInt(target.value + 1);
        public static UVarInt operator --(UVarInt target) => new UVarInt(target.value - 1);
        #endregion

        #region Conversions
        //From VarInt
        public static implicit operator ulong(UVarInt target) => target.value;
        public static implicit operator uint(UVarInt target) => (uint)target.value;

        //To VarInt
        public static implicit operator UVarInt(ulong value) => new UVarInt(value);
        public static implicit operator UVarInt(uint value) => new UVarInt(value);
        public static implicit operator UVarInt(ushort value) => new UVarInt(value);
        public static implicit operator UVarInt(byte value) => new UVarInt(value);
        #endregion

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(UVarInt))]
        public class Drawer : BaseDrawer { }
#endif
    }
}