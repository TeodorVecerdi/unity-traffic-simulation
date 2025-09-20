// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the THIRD-PARTY-NOTICES file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TrafficSimulation.Core.Text {
    public ref struct ValueStringBuilder {
        private char[]? m_ArrayToReturnToPool;
        private Span<char> m_Chars;
        private int m_Pos;

        public ValueStringBuilder(Span<char> initialBuffer) {
            m_ArrayToReturnToPool = null;
            m_Chars = initialBuffer;
            m_Pos = 0;
        }

        public ValueStringBuilder(int initialCapacity) {
            m_ArrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            m_Chars = m_ArrayToReturnToPool;
            m_Pos = 0;
        }

        public int Length {
            get => m_Pos;
            set {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= m_Chars.Length);
                m_Pos = value;
            }
        }

        public int Capacity => m_Chars.Length;

        public void EnsureCapacity(int capacity) {
            // This is not expected to be called this with negative capacity
            Debug.Assert(capacity >= 0);

            // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
            if ((uint)capacity > (uint)m_Chars.Length)
                Grow(capacity - m_Pos);
        }

        /// <summary>
        /// Get a pinnable reference to the builder.
        /// Does not ensure there is a null char after <see cref="Length"/>
        /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
        /// the explicit method call, and write eg "fixed (char* c = builder)"
        /// </summary>
        public ref char GetPinnableReference() {
            return ref MemoryMarshal.GetReference(m_Chars);
        }

        /// <summary>
        /// Get a pinnable reference to the builder.
        /// </summary>
        /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
        public ref char GetPinnableReference(bool terminate) {
            if (terminate) {
                EnsureCapacity(Length + 1);
                m_Chars[Length] = '\0';
            }

            return ref MemoryMarshal.GetReference(m_Chars);
        }

        public ref char this[int index] {
            get {
                Debug.Assert(index < m_Pos);
                return ref m_Chars[index];
            }
        }

        public override string ToString() {
            var s = m_Chars[..m_Pos].ToString();
            Dispose();
            return s;
        }

        /// <summary>Returns the underlying storage of the builder.</summary>
        public Span<char> RawChars => m_Chars;

        /// <summary>
        /// Returns a span around the contents of the builder.
        /// </summary>
        /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
        public ReadOnlySpan<char> AsSpan(bool terminate) {
            if (terminate) {
                EnsureCapacity(Length + 1);
                m_Chars[Length] = '\0';
            }

            return m_Chars[..m_Pos];
        }

        public ReadOnlySpan<char> AsSpan() {
            return m_Chars[..m_Pos];
        }

        public ReadOnlySpan<char> AsSpan(int start) {
            return m_Chars.Slice(start, m_Pos - start);
        }

        public ReadOnlySpan<char> AsSpan(int start, int length) {
            return m_Chars.Slice(start, length);
        }

        public bool TryCopyTo(Span<char> destination, out int charsWritten) {
            if (m_Chars[..m_Pos].TryCopyTo(destination)) {
                charsWritten = m_Pos;
                Dispose();
                return true;
            }

            charsWritten = 0;
            Dispose();
            return false;
        }

        public void Insert(int index, char value, int count) {
            if (m_Pos > m_Chars.Length - count) {
                Grow(count);
            }

            var remaining = m_Pos - index;
            m_Chars.Slice(index, remaining).CopyTo(m_Chars[(index + count)..]);
            m_Chars.Slice(index, count).Fill(value);
            m_Pos += count;
        }

        public void Insert(int index, string? s) {
            if (s == null) {
                return;
            }

            var count = s.Length;

            if (m_Pos > m_Chars.Length - count) {
                Grow(count);
            }

            var remaining = m_Pos - index;
            m_Chars.Slice(index, remaining).CopyTo(m_Chars[(index + count)..]);
            s
#if !NET
                .AsSpan()
#endif
                .CopyTo(m_Chars[index..]);
            m_Pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c) {
            var pos = m_Pos;
            var chars = m_Chars;
            if ((uint)pos < (uint)chars.Length) {
                chars[pos] = c;
                m_Pos = pos + 1;
            } else {
                GrowAndAppend(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string? s) {
            if (s == null) {
                return;
            }

            var pos = m_Pos;
            if (s.Length == 1 && (uint)pos < (uint)m_Chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
            {
                m_Chars[pos] = s[0];
                m_Pos = pos + 1;
            } else {
                AppendSlow(s);
            }
        }

        private void AppendSlow(string s) {
            var pos = m_Pos;
            if (pos > m_Chars.Length - s.Length) {
                Grow(s.Length);
            }

            s
#if !NET
                .AsSpan()
#endif
                .CopyTo(m_Chars[pos..]);
            m_Pos += s.Length;
        }

        public void Append(char c, int count) {
            if (m_Pos > m_Chars.Length - count) {
                Grow(count);
            }

            var dst = m_Chars.Slice(m_Pos, count);
            for (var i = 0; i < dst.Length; i++) {
                dst[i] = c;
            }

            m_Pos += count;
        }

        public void Append(ReadOnlySpan<char> value) {
            var pos = m_Pos;
            if (pos > m_Chars.Length - value.Length) {
                Grow(value.Length);
            }

            value.CopyTo(m_Chars[m_Pos..]);
            m_Pos += value.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<char> AppendSpan(int length) {
            var origPos = m_Pos;
            if (origPos > m_Chars.Length - length) {
                Grow(length);
            }

            m_Pos = origPos + length;
            return m_Chars.Slice(origPos, length);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppend(char c) {
            Grow(1);
            Append(c);
        }

        /// <summary>
        /// Resize the internal buffer either by doubling current buffer size or
        /// by adding <paramref name="additionalCapacityBeyondPos"/> to
        /// <see cref="m_Pos"/> whichever is greater.
        /// </summary>
        /// <param name="additionalCapacityBeyondPos">
        /// Number of chars requested beyond current position.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int additionalCapacityBeyondPos) {
            Debug.Assert(additionalCapacityBeyondPos > 0);
            Debug.Assert(m_Pos > m_Chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

            const uint arrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

            // Increase to at least the required size (_pos + additionalCapacityBeyondPos), but try
            // to double the size if possible, bounding the doubling to not go beyond the max array length.
            var newCapacity = (int)Math.Max(
                (uint)(m_Pos + additionalCapacityBeyondPos),
                Math.Min((uint)m_Chars.Length * 2, arrayMaxLength));

            // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative.
            // This could also go negative if the actual required length wraps around.
            var poolArray = ArrayPool<char>.Shared.Rent(newCapacity);

            m_Chars[..m_Pos].CopyTo(poolArray);

            var toReturn = m_ArrayToReturnToPool;
            m_Chars = m_ArrayToReturnToPool = poolArray;
            if (toReturn != null) {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            var toReturn = m_ArrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null) {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }
    }
}
