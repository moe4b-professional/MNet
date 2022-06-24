using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using Cysharp.Threading.Tasks;

using System.Threading;
using System.Threading.Tasks;
using System.Text;

using MB;
using System.Reflection;
using System.Runtime.InteropServices;

using MNet;
using System.Collections.Concurrent;
using Replacement;
using System.Buffers;

namespace MNet.Example
{
    [Preserve]
    public class Sandbox : NetworkBehaviour
    {
        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
        {
            
        }

#if UNITY_EDITOR
        [MenuItem("Sandbox/Execute")]
        static void Execute()
        {
            Span<byte> buffer = stackalloc byte[512];

            var writer = new NetworkSpanWriter(buffer);

            writer.Insert(new byte[] { 1, 2, 3, 4, 5 });

            var span = writer.AsSpan();

            for (int i = 0; i < span.Length; i++)
                Debug.Log(span[i]);

            var reader = new NetworkSpanReader(span);

            for (int i = 0; i < 5; i++)
            {
                var value = reader.TakeByte();
                Debug.Log(value);
            }
        }
#endif
    }

    public struct PooledBuffer : IDisposable
    {
        public byte[] Data { get; }
        public int Length { get; }

        public ArraySegment<byte> ToSegment() => new ArraySegment<byte>(Data, 0, Length);
        public Span<byte> ToSpan() => new Span<byte>(Data, 0, Length);

        static System.Buffers.ArrayPool<byte> ArrayPool => System.Buffers.ArrayPool<byte>.Shared;

        public void Dispose()
        {
            ArrayPool.Return(Data);
        }

        internal PooledBuffer(byte[] data, int length)
        {
            this.Data = data;
            this.Length = length;
        }

        public static PooledBuffer Create(int length)
        {
            var buffer = ArrayPool.Rent(length);

            return new PooledBuffer(buffer, length);
        }
        public static PooledBuffer Create(ReadOnlySpan<byte> span)
        {
            var buffer = ArrayPool.Rent(span.Length);
            span.CopyTo(buffer);

            return new PooledBuffer(buffer, span.Length);
        }
    }

    public class PooledBufferSerializationResolver : ExplicitNetworkSerializationResolver<PooledBuffer>
    {
        public override void Serialize(NetworkWriter writer, PooledBuffer instance)
        {
            NetworkSerializationHelper.Length.Write(writer, instance.Length);

            if (instance.Length > 0)
            {
                var span = instance.ToSpan();
                writer.Insert(span);
            }
        }
        public override PooledBuffer Deserialize(NetworkReader reader)
        {
            var length = NetworkSerializationHelper.Length.Read(reader);

            if (length == 0)
                return new PooledBuffer(Array.Empty<byte>(), length);

            var span = reader.TakeSpan(length);
            return PooledBuffer.Create(span);
        }
    }
}

namespace Replacement
{
    public ref struct NetworkSpanReader
    {
        Span<byte> data;
        public Span<byte> Data => data;

        /// <summary>
        /// The Available Binary Capacity
        /// </summary>
        public int Capacity => data.Length;

        int internal_position;
        /// <summary>
        /// The Current Position of the Stream
        /// </summary>
        public int Position
        {
            get => internal_position;
            set
            {
                if (value < 0 || value > Capacity)
                    throw new IndexOutOfRangeException();

                internal_position = value;
            }
        }

        /// <summary>
        /// The Remaining Amount of Capacity
        /// </summary>
        public int Remaining => Capacity - Position;

        #region To Array
        /// <summary>
        /// Clones the Stream to a Byte Array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray() => ToArray(0, Position);
        public byte[] ToArray(int offset, int count)
        {
            var destination = new byte[count];
            var span = new Span<byte>(destination);

            var slice = data.Slice(offset, count);
            slice.CopyTo(span);

            return destination;
        }
        #endregion

        public NetworkSpanReader(Span<byte> data)
        {
            this.data = data;
            internal_position = 0;
        }

        //Static Utility

        public static NotImplementedException FormatResolverException<T>()
        {
            var type = typeof(T);

            return FormatResolverException(type);
        }
        public static NotImplementedException FormatResolverException(Type type)
        {
            return new NotImplementedException($"Type ({type}) isn't supported for Network Serialization");
        }

        #region Take
        /// <summary>
        /// Retrieves the Next Byte in Stream and Iterates the Position by 1
        /// </summary>
        /// <returns></returns>
        public byte TakeByte()
        {
            Position += 1;

            return data[Position - 1];
        }

        /// <summary>
        /// Retrieves the Next Bytes in Memory and Iterates the Position by the Length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public byte[] TakeArray(int length)
        {
            var raw = ToArray(Position, length);
            Position += length;

            return raw;
        }

        /// <summary>
        /// Retrieves the Next Bytes in Memory and Iterates the Position by the Length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public Span<byte> TakeSpan(int length)
        {
            var slice = data.Slice(Position, length);

            Position += length;

            return slice;
        }
        #endregion
    }

    public ref struct NetworkSpanWriter
    {
        Span<byte> data;
        public Span<byte> Data => data;

        /// <summary>
        /// The Available Binary Capacity
        /// </summary>
        public int Capacity => data.Length;

        int internal_position;
        /// <summary>
        /// The Current Position of the Stream
        /// </summary>
        public int Position
        {
            get => internal_position;
            set
            {
                if (value < 0 || value > Capacity)
                    throw new IndexOutOfRangeException();

                internal_position = value;
            }
        }

        /// <summary>
        /// The Remaining Amount of Capacity
        /// </summary>
        public int Remaining => Capacity - Position;

        /// <summary>
        /// Resets the Stream State (Position)
        /// </summary>
        public void Reset()
        {
            Position = 0;
        }

        #region To Array
        /// <summary>
        /// Clones the Stream to a Byte Array
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray() => ToArray(0, Position);
        public byte[] ToArray(int offset, int count)
        {
            var destination = new byte[count];
            var span = new Span<byte>(destination);

            var slice = data.Slice(offset, count);
            slice.CopyTo(span);

            return destination;
        }

        /// <summary>
        /// Clones the Stream to a Byte Array and Resets it
        /// </summary>
        /// <returns></returns>
        public byte[] Flush()
        {
            var raw = ToArray();

            Reset();

            return raw;
        }
        #endregion

        /// <summary>
        /// Recycles the Stream
        /// </summary>
        public void Dispose() => Reset();

        public NetworkSpanWriter(Span<byte> data)
        {
            this.data = data;
            internal_position = 0;
        }

        //Static Utility

        public static NotImplementedException FormatResolverException<T>()
        {
            var type = typeof(T);

            return FormatResolverException(type);
        }
        public static NotImplementedException FormatResolverException(Type type)
        {
            return new NotImplementedException($"Type ({type}) isn't supported for Network Serialization");
        }

        #region Slicing
        public Span<byte> AsSpan() => AsSpan(0, Position);
        public Span<byte> AsSpan(int offset, int count) => data.Slice(offset, count);
        #endregion

        void Fit(int length)
        {
            if (length > Remaining)
                throw new InvalidOperationException("Cannot Fit More Data in Stream");
        }

        #region Insert
        public void Insert(byte value)
        {
            Fit(1);

            data[Position] = value;

            Position += 1;
        }

        public void Insert(Span<byte> span)
        {
            Fit(span.Length);

            var slice = data.Slice(Position, span.Length);
            span.CopyTo(slice);

            Position += span.Length;
        }

        public void Insert(Stream stream) => Insert(stream, (int)(stream.Length));
        public void Insert(Stream stream, int count)
        {
            Fit(count);

            while(count > 0)
            {
                var slice = data.Slice(Position, count);

                var read = stream.Read(slice);

                count -= read;
                Position += read;
            }
        }
        #endregion
    }
}