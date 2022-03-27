using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    /// <summary>
    /// A segment of byte memory that is captured from the serializing network stream,
    /// valid for as long as the network stream is not reused
    /// as it basically shares the lifetime of the stream that deserialized it
    /// very dangerous otherwise
    /// </summary>
    public struct ByteChunk : IManualNetworkSerializable
    {
        public byte[] Array { get; private set; }

        public int Offset { get; private set; }
        public int Count { get; private set; }

        public byte this[int index] => Array[index + Offset];

        public void Serialize(NetworkWriter writer)
        {
            NetworkSerializationHelper.Length.Write(writer, Count);
            writer.Insert(Array, Offset, Count);
        }
        public void Deserialize(NetworkReader reader)
        {
            Count = NetworkSerializationHelper.Length.Read(reader);

            Array = reader.Data;
            Offset = reader.Position;

            reader.Position += Count;
        }

        public ByteChunk(byte[] array, int offset, int count)
        {
            this.Array = array;
            this.Offset = offset;
            this.Count = count;
        }
        public ByteChunk(byte[] array) : this(array, 0, array.Length) { }

        /// <summary>
        /// Clones the ByteChunk in memory by allocating it's contents so it's no longer dangerous to use
        /// </summary>
        /// <returns></returns>
        public ByteChunk Clone()
        {
            var destination = new byte[Count];

            Buffer.BlockCopy(Array, Offset, destination, 0, Count);

            return new ByteChunk(destination);
        }
    }
}