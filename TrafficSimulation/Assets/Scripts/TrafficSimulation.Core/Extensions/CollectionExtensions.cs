namespace TrafficSimulation.Core.Extensions;

public static class CollectionExtensions {
    public static T? GetValueOrDefault<T>(this IReadOnlyList<T> collection, int index) {
        return index >= 0 && index < collection.Count ? collection[index] : default;
    }

    public static int IndexOf<T>(this IReadOnlyList<T> collection, T value, IEqualityComparer<T>? comparer = null) {
        comparer ??= EqualityComparer<T>.Default;
        var index = -1;
        for (var i = 0; i < collection.Count; i++) {
            if (comparer.Equals(collection[i], value)) {
                index = i;
                break;
            }
        }

        return index;
    }
}
