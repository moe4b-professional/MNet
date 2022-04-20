using System;
using System.Net;
using System.Numerics;

namespace MNet
{
    public ref struct SpanStringBuilder
    {
        Span<char> characters;
        int size;
        int position;

        int Remaining => size - position;

        #region Append
        public void NewLine() => Append('\n');

        public void Space() => Append(' ');

        public void Append(ReadOnlySpan<char> characters)
        {
            var target = Take(characters.Length);
            characters.CopyTo(target);
        }

        public void Append(bool target)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(char character)
        {
            CheckLength(1);
            characters[position] = character;
            position += 1;
        }

        public void Append(byte target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        public void Append(sbyte target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(short target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        public void Append(ushort target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(int target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        public void Append(uint target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(long target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        public void Append(ulong target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(BigInteger target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(float target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        public void Append(double target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        public void Append(decimal target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(Guid target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(DateTime target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(DateTimeOffset target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(TimeSpan target, ReadOnlySpan<char> format = default, IFormatProvider provider = default)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written, format, provider) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }

        public void Append(IPAddress target)
        {
            var span = Buffer();

            if (target.TryFormat(span, out var written) == false)
                throw new InvalidOperationException($"Cannot Write Formattable Target ({target}), Not Enough Space");

            position += written;
        }
        #endregion

        #region Slicing
        Span<char> Take(int length)
        {
            CheckLength(length);

            var span = characters.Slice(position, length);

            position += length;

            return span;
        }

        Span<char> Buffer()
        {
            var span = characters.Slice(position, Remaining);

            return span;
        }

        void CheckLength(int length)
        {
            if (length > Remaining)
                throw new InvalidOperationException("Cannot Allocate any More Characters");
        }
        #endregion

        public ReadOnlySpan<char> ToSpan()
        {
            return characters.Slice(0, position);
        }
        public override string ToString()
        {
            var span = ToSpan();
            return new string(span);
        }

        public SpanStringBuilder(Span<char> characters)
        {
            this.characters = characters;
            this.size = characters.Length;

            position = 0;
        }
    }
}