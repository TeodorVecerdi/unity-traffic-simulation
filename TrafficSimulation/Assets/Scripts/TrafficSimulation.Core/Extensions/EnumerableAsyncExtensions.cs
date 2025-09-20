namespace TrafficSimulation.Core.Extensions;

public static class EnumerableAsyncExtensions {
    public static async UniTask<List<T>> ToList<T>(this UniTask<IEnumerable<T>> source) {
        return (await source).ToList();
    }

    public static async UniTask<List<T>> ToList<T>(this UniTask<IOrderedEnumerable<T>> source) {
        return (await source).ToList();
    }

    public static async UniTask<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this UniTask<IEnumerable<TSource>> source, Func<TSource, UniTask<TResult>> selector) {
        return await (await source).Select(selector);
    }

    public static async UniTask<IEnumerable<TResult>> SelectAsync<TSource, TResult>(this UniTask<IEnumerable<TSource>> source, Func<TSource, TResult> selector) {
        return (await source).Select(selector);
    }

    public static async UniTask<IEnumerable<T>> WhereAsync<T>(this UniTask<IEnumerable<T>> source, Func<T, bool> predicate) {
        return (await source).Where(predicate);
    }

    public static async UniTask<IOrderedEnumerable<T>> OrderByAsync<T>(this UniTask<IEnumerable<T>> source, Func<T, IComparable> keySelector) {
        return (await source).OrderBy(keySelector);
    }

    public static async UniTask<IOrderedEnumerable<T>> OrderByDescendingAsync<T>(this UniTask<IEnumerable<T>> source, Func<T, IComparable> keySelector) {
        return (await source).OrderByDescending(keySelector);
    }

    public static async UniTask<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(this UniTask<IEnumerable<TValue>> source, Func<TValue, TKey> keySelector) {
        return (await source).ToDictionary(keySelector);
    }

    public static async UniTask<Dictionary<TKey, TValue>> ToDictionaryAsync<TSource, TKey, TValue>(this UniTask<IEnumerable<TSource>> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector) {
        return (await source).ToDictionary(keySelector, elementSelector);
    }
}
