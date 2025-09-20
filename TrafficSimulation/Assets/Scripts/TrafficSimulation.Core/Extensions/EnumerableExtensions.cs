using System.Runtime.CompilerServices;

namespace TrafficSimulation.Core.Extensions;

public static class EnumerableExtensions {
    public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source) {
        // ReSharper disable PossibleMultipleEnumeration
        return IsEmptyArray(source) ? (IEnumerable<(int, TSource)>)[] : IndexIterator(source);
        // ReSharper restore PossibleMultipleEnumeration
    }

    public static IEnumerable<T> AppendIfNotNull<T>(this IEnumerable<T> source, T? item) {
        return item is not null ? source.Append(item) : source;
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
        foreach (var item in source) {
            action(item);
        }
    }

#nullable disable
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEmptyArray<TSource>(IEnumerable<TSource> source) {
        return source is TSource[] { Length: 0 };
    }

    private static IEnumerable<(int Index, TSource Item)> IndexIterator<TSource>(IEnumerable<TSource> source) {
        var index = -1;
        foreach (var item in source) {
            checked { ++index; }

            yield return (index, item);
        }
    }
    // ReSharper disable once UnusedNullableDirective
#nullable enable
}
