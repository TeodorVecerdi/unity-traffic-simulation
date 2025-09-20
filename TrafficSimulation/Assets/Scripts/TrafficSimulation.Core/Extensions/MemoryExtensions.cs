using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace TrafficSimulation.Core.Extensions;

public static class MemoryExtensions {
    /// <summary>
    /// Returns a type that allows for enumeration of each element within a split span
    /// using the provided separator character.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The source span to be enumerated.</param>
    /// <param name="separator">The separator character to be used to split the provided span.</param>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
    public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> source, T separator) where T : IEquatable<T>
        => new(source, separator);

    /// <summary>
    /// Returns a type that allows for enumeration of each element within a split span
    /// using the provided separator span.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The source span to be enumerated.</param>
    /// <param name="separator">The separator span to be used to split the provided span.</param>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
    public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> separator) where T : IEquatable<T>
        => new(source, separator, treatAsSingleSeparator: true);

    /// <summary>
    /// Returns a type that allows for enumeration of each element within a split span
    /// using any of the provided elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <param name="source">The source span to be enumerated.</param>
    /// <param name="separators">The separators to be used to split the provided span.</param>
    /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
    public static SpanSplitEnumerator<T> SplitAny<T>(this ReadOnlySpan<T> source, [UnscopedRef] params ReadOnlySpan<T> separators) where T : IEquatable<T>
        => new(source, separators);

    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T> {
        /// <summary>The input span being split.</summary>
        private readonly ReadOnlySpan<T> m_Span;

        /// <summary>A single separator to use when <see cref="m_SplitMode"/> is <see cref="SpanSplitEnumeratorMode.SingleElement"/>.</summary>
        private readonly T m_Separator = default!;
        /// <summary>
        /// A separator span to use when <see cref="m_SplitMode"/> is <see cref="SpanSplitEnumeratorMode.Sequence"/> (in which case
        /// it's treated as a single separator) or <see cref="SpanSplitEnumeratorMode.Any"/> (in which case it's treated as a set of separators).
        /// </summary>
        private readonly ReadOnlySpan<T> m_SeparatorBuffer;

        /// <summary>Mode that dictates how the instance was configured and how its fields should be used in <see cref="MoveNext"/>.</summary>
        private SpanSplitEnumeratorMode m_SplitMode;
        /// <summary>The inclusive starting index in <see cref="m_Span"/> of the current range.</summary>
        private int m_StartCurrent = 0;
        /// <summary>The exclusive ending index in <see cref="m_Span"/> of the current range.</summary>
        private int m_EndCurrent = 0;
        /// <summary>The index in <see cref="m_Span"/> from which the next separator search should start.</summary>
        private int m_StartNext = 0;

        /// <summary>Gets an enumerator that allows for iteration over the split span.</summary>
        /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
        public SpanSplitEnumerator<T> GetEnumerator() => this;

        /// <summary>Gets the current element of the enumeration.</summary>
        /// <returns>Returns a <see cref="Range"/> instance that indicates the bounds of the current element withing the source span.</returns>
        public Range Current => new(m_StartCurrent, m_EndCurrent);

        /// <summary>Initializes the enumerator for <see cref="SpanSplitEnumeratorMode.Any"/>.</summary>
        internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators) {
            m_Span = span;
            m_SeparatorBuffer = separators;
            m_SplitMode = SpanSplitEnumeratorMode.Any;
        }

        /// <summary>Initializes the enumerator for <see cref="SpanSplitEnumeratorMode.Sequence"/> (or <see cref="SpanSplitEnumeratorMode.EmptySequence"/> if the separator is empty).</summary>
        /// <remarks><paramref name="treatAsSingleSeparator"/> must be true.</remarks>
        internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separator, bool treatAsSingleSeparator) {
            _ = treatAsSingleSeparator;
            Debug.Assert(treatAsSingleSeparator, "Should only ever be called as true; exists to differentiate from separators overload");

            m_Span = span;
            m_SeparatorBuffer = separator;
            m_SplitMode = separator.Length == 0 ? SpanSplitEnumeratorMode.EmptySequence : SpanSplitEnumeratorMode.Sequence;
        }

        /// <summary>Initializes the enumerator for <see cref="SpanSplitEnumeratorMode.SingleElement"/>.</summary>
        internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator) {
            m_Span = span;
            m_Separator = separator;
            m_SplitMode = SpanSplitEnumeratorMode.SingleElement;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the enumeration.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the enumeration.</returns>
        public bool MoveNext() {
            // Search for the next separator index.
            int separatorIndex, separatorLength;
            switch (m_SplitMode) {
                case SpanSplitEnumeratorMode.None:
                    return false;

                case SpanSplitEnumeratorMode.SingleElement:
                    separatorIndex = m_Span.Slice(m_StartNext).IndexOf(m_Separator);
                    separatorLength = 1;
                    break;

                case SpanSplitEnumeratorMode.Any:
                    separatorIndex = m_Span.Slice(m_StartNext).IndexOfAny(m_SeparatorBuffer);
                    separatorLength = 1;
                    break;

                case SpanSplitEnumeratorMode.Sequence:
                    separatorIndex = m_Span.Slice(m_StartNext).IndexOf(m_SeparatorBuffer);
                    separatorLength = m_SeparatorBuffer.Length;
                    break;

                case SpanSplitEnumeratorMode.EmptySequence:
                    separatorIndex = -1;
                    separatorLength = 1;
                    break;

                default: throw new InvalidOperationException("Invalid split mode.");
            }

            m_StartCurrent = m_StartNext;
            if (separatorIndex >= 0) {
                m_EndCurrent = m_StartCurrent + separatorIndex;
                m_StartNext = m_EndCurrent + separatorLength;
            } else {
                m_StartNext = m_EndCurrent = m_Span.Length;

                // Set _splitMode to None so that subsequent MoveNext calls will return false.
                m_SplitMode = SpanSplitEnumeratorMode.None;
            }

            return true;
        }
    }

    /// <summary>Indicates in which mode <see cref="SpanSplitEnumerator{T}"/> is operating, with regards to how it should interpret its state.</summary>
    private enum SpanSplitEnumeratorMode {
        /// <summary>Either a default <see cref="SpanSplitEnumerator{T}"/> was used, or the enumerator has finished enumerating and there's no more work to do.</summary>
        None = 0,

        /// <summary>A single T separator was provided.</summary>
        SingleElement,

        /// <summary>A span of separators was provided, each of which should be treated independently.</summary>
        Any,

        /// <summary>The separator is a span of elements to be treated as a single sequence.</summary>
        Sequence,

        /// <summary>The separator is an empty sequence, such that no splits should be performed.</summary>
        EmptySequence,
    }
}
