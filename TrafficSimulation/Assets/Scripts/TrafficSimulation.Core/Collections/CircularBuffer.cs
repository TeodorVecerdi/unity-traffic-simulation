using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TrafficSimulation.Core.Collections;

public sealed class CircularBuffer<T> : ICollection<T>, IReadOnlyCollection<T> {
    private readonly T[] m_Buffer;
    private int m_Start;
    private int m_End;
    private int m_Size;

    public CircularBuffer(int capacity) {
        if (capacity < 1) {
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
        }

        m_Buffer = new T[capacity];
        m_Size = 0;
        m_Start = 0;
        m_End = 0;
    }

    /// <summary>
    /// Gets a value representing the number of elements in the buffer.
    /// </summary>
    public int Count => m_Size;

    /// <summary>
    /// Gets a value representing the capacity of the buffer.
    /// </summary>
    public int Capacity => m_Buffer.Length;

    /// <summary>
    /// Gets a value indicating whether the buffer is empty.
    /// </summary>
    public bool IsEmpty => m_Size == 0;

    /// <summary>
    /// Gets a value indicating whether the buffer is full.
    /// </summary>
    public bool IsFull => m_Size == m_Buffer.Length;

    /// <summary>Gets a value indicating whether the <see cref="CircularBuffer{T}" /> is read-only.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="CircularBuffer{T}" /> is read-only; otherwise, <see langword="false" />.</returns>
    public bool IsReadOnly => false;

    /// <summary>
    /// Returns the element at the front of the buffer.
    /// </summary>
    /// <returns>The element at the front of the buffer.</returns>
    public T Front() {
        ThrowIfEmpty();
        return m_Buffer[m_Start];
    }

    /// <summary>
    /// Returns the element at the back of the buffer.
    /// </summary>
    /// <returns>The element at the back of the buffer.</returns>
    public T Back() {
        ThrowIfEmpty();
        return m_Buffer[(m_End != 0 ? m_End : m_Buffer.Length) - 1];
    }

    /// <summary>
    /// Adds an element to the back of the buffer.
    /// </summary>
    /// <param name="item">The element to add.</param>
    public void PushBack(T item) {
        if (IsFull) {
            m_Buffer[m_End] = item;
            Increment(ref m_End);
            m_Start = m_End;
        } else {
            m_Buffer[m_End] = item;
            Increment(ref m_End);
            m_Size++;
        }
    }

    /// <summary>
    /// Adds the elements of the specified collection to the back of the <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <param name="items">The collection whose elements should be added to the end of the <see cref="CircularBuffer{T}" />.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="items" /> is <see langword="null" />.</exception>
    public void PushBack(T[] items) {
        if (items is null) {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Length == 0) {
            return;
        }

        if (items.Length >= m_Buffer.Length) {
            // If items to add is larger than or equal to buffer size, just copy the last 'm_Buffer.Length' items
            Array.Copy(items, items.Length - m_Buffer.Length, m_Buffer, 0, m_Buffer.Length);
            m_Start = 0;
            m_End = 0;
            m_Size = m_Buffer.Length;
            return;
        }

        var itemsToAdd = items.Length;
        var spaceLeft = m_Buffer.Length - m_Size;

        if (itemsToAdd > spaceLeft) {
            // Adjust the start pointer if we're overwriting existing elements
            m_Start = (m_Start + (itemsToAdd - spaceLeft)) % m_Buffer.Length;
        }

        var endCount = m_Buffer.Length - m_End;
        var copyEndCount = Math.Min(endCount, itemsToAdd);
        var copyStartCount = itemsToAdd - copyEndCount;

        // Copy to the end of the buffer
        Array.Copy(items, 0, m_Buffer, m_End, copyEndCount);
        m_End = (m_End + copyEndCount) % m_Buffer.Length;

        // Copy remaining items to the start of the buffer if necessary
        if (copyStartCount > 0) {
            Array.Copy(items, copyEndCount, m_Buffer, m_End, copyStartCount);
            m_End = (m_End + copyStartCount) % m_Buffer.Length;
        }

        // Update size
        m_Size = Math.Min(m_Size + itemsToAdd, m_Buffer.Length);
    }

    /// <summary>
    /// Adds an element to the front of the buffer.
    /// </summary>
    /// <param name="item">The element to add.</param>
    public void PushFront(T item) {
        if (m_Size == m_Buffer.Length) {
            Decrement(ref m_Start);
            m_End = m_Start;
            m_Buffer[m_Start] = item;
        } else {
            Decrement(ref m_Start);
            m_Buffer[m_Start] = item;
            m_Size++;
        }
    }

    /// <summary>
    /// Removes the element at the back of the buffer.
    /// </summary>
    /// <returns>The element at the back of the buffer.</returns>
    public T PopBack() {
        ThrowIfEmpty();
        Decrement(ref m_End);
        m_Size--;
        return m_Buffer[m_End];
    }

    /// <summary>
    /// Removes the element at the front of the buffer.
    /// </summary>
    /// <returns>The element at the front of the buffer.</returns>
    public T PopFront() {
        ThrowIfEmpty();
        var item = m_Buffer[m_Start];
        Increment(ref m_Start);
        m_Size--;
        return item;
    }

    /// <summary>
    /// This method is not supported.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="CircularBuffer{T}" />.</param>
    /// <exception cref="T:System.NotSupportedException">This method is not supported.</exception>
    /// <returns>This method always throws a <see cref="T:System.NotSupportedException" />.</returns>
    [DoesNotReturn]
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    /// <summary>
    /// Returns the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index] {
        get {
            if (index < 0 || index >= m_Size) {
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {m_Size}).");
            }

            var actualIndex = CalculateIndex(index);
            return m_Buffer[actualIndex];
        }
        set {
            if (index < 0 || index >= m_Size) {
                throw new IndexOutOfRangeException($"Index {index} is out of range [0, {m_Size}).");
            }

            var actualIndex = CalculateIndex(index);
            m_Buffer[actualIndex] = value;
        }
    }

    /// <summary>
    /// Adds an item to the back of the <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <param name="item">The object to add to the back of the <see cref="CircularBuffer{T}" />.</param>
    public void Add(T item) {
        PushBack(item);
    }

    /// <summary>
    /// Adds the elements of the specified collection to the back of the <see cref="CircularBuffer{T}" />.
    /// </summary>
    /// <param name="items">The collection whose elements should be added to the end of the <see cref="CircularBuffer{T}" />.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="items" /> is <see langword="null" />.</exception>
    public void AddRange(IEnumerable<T> items) {
        if (items is null) {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items) {
            PushBack(item);
        }
    }

    /// <summary>
    /// Removes all items from the <see cref="CircularBuffer{T}" />.
    /// </summary>
    public void Clear() {
        m_Size = 0;
        m_Start = 0;
        m_End = 0;
    }

    /// <summary>
    /// Determines whether the <see cref="CircularBuffer{T}" /> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="CircularBuffer{T}" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="item" /> is found in the <see cref="CircularBuffer{T}" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public bool Contains(T item) {
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Size; i++) {
            if (comparer.Equals(this[i], item)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Copies the elements of the <see cref="CircularBuffer{T}" /> to an <see cref="T:System.Array" />,
    /// starting at a particular <see cref="T:System.Array" /> index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="CircularBuffer{T}" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than 0.</exception>
    /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="CircularBuffer{T}" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
    public void CopyTo(T[] array, int arrayIndex) {
        if (array is null) {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0) {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Array index must be non-negative.");
        }

        if (array.Length - arrayIndex < m_Size) {
            throw new ArgumentException("Destination array is not large enough to copy all the items in the collection.");
        }

        var (front, back) = GetSegments();
        front.CopyTo(array.AsMemory(arrayIndex));
        back.CopyTo(array.AsMemory(arrayIndex + front.Length));
    }

    /// <summary>
    /// Returns a copy of the buffer as an array.
    /// </summary>
    /// <returns>A copy of the buffer as an array.</returns>
    public T[] ToArray() {
        if (m_Size is 0) return [];

        var array = new T[m_Size];
        var (front, back) = GetSegments();

        front.CopyTo(array.AsMemory());
        back.CopyTo(array.AsMemory(front.Length));

        return array;
    }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public (Memory<T> Front, Memory<T> Back) GetSegments() {
        return (
            GetFrontSegment(),
            GetBackSegment()
        );
    }

    private void Increment(ref int value) {
        if (++value == m_Buffer.Length) {
            value = 0;
        }
    }

    private void Decrement(ref int value) {
        if (--value == -1) {
            value = m_Buffer.Length - 1;
        }
    }

    private int CalculateIndex(int index) {
        return m_Start + (index < m_Buffer.Length - m_Start ? index : index - m_Buffer.Length);
    }

    private void ThrowIfEmpty(string message = "The buffer is empty.") {
        if (m_Size == 0) {
            throw new InvalidOperationException(message);
        }
    }

    private Memory<T> GetFrontSegment() {
        if (m_Size is 0) {
            return Memory<T>.Empty;
        }

        if (m_Start < m_End) {
            return new Memory<T>(m_Buffer, m_Start, m_End - m_Start);
        }

        return new Memory<T>(m_Buffer, m_Start, m_Buffer.Length - m_Start);
    }

    private Memory<T> GetBackSegment() {
        if (m_Size is 0) {
            return Memory<T>.Empty;
        }

        if (m_Start < m_End) {
            return Memory<T>.Empty;
        }

        return new Memory<T>(m_Buffer, 0, m_End);
    }

    public struct Enumerator : IEnumerator<T> {
        private readonly Memory<T> m_Front;
        private readonly Memory<T> m_Back;

        private int m_Index;
        private int m_End;

        private Memory<T> m_CurrentSegment;
        private int m_CurrentIndex;

        public Enumerator(CircularBuffer<T> buffer) {
            m_Index = 0;
            m_End = buffer.m_Size;

            (m_Front, m_Back) = buffer.GetSegments();
            m_CurrentSegment = m_Front;
            m_CurrentIndex = -1;
        }

        public T Current => m_CurrentSegment.Span[m_CurrentIndex];
        object? IEnumerator.Current => Current;

        public bool MoveNext() {
            if (m_Index >= m_End) return false;
            m_Index++;
            m_CurrentIndex++;

            if (m_CurrentIndex < m_CurrentSegment.Length) {
                return true;
            }

            m_CurrentIndex = 0;
            m_CurrentSegment = m_Back;
            return m_CurrentIndex < m_CurrentSegment.Length;
        }

        public void Reset() {
            m_Index = 0;
            m_CurrentIndex = -1;
            m_CurrentSegment = m_Front;
        }

        public void Dispose() { }
    }
}
