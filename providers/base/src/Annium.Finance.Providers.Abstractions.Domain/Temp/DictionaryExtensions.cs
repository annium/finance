using System;
using System.Collections.Generic;

namespace Annium.Finance.Providers.Abstractions.Domain.Temp;

// TODO: remove temp
public static class DictionaryExtensions
{
    /// <summary>
    /// Merges collection of key-value pairs into a given dictionary, with values from the target collection taking precedence.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the collections.</typeparam>
    /// <typeparam name="TValue">The type of the values in the collections.</typeparam>
    /// <param name="source">The source collection of key-value pairs.</param>
    /// <param name="target">The target collection of key-value pairs whose values will override source values for matching keys.</param>
    /// <param name="mergeBehavior">Merge behavior</param>
    /// <returns>A new dictionary containing the merged key-value pairs.</returns>
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        this Dictionary<TKey, TValue> source,
        IEnumerable<KeyValuePair<TKey, TValue>> target,
        MergeBehavior mergeBehavior
    )
        where TKey : notnull
    {
        if (mergeBehavior is MergeBehavior.KeepTarget)
            foreach (var (key, value) in target)
                source[key] = value;
        else
            foreach (var (key, value) in target)
                source.TryAdd(key, value);

        return source;
    }

    /// <summary>
    /// Merges two collections of key-value pairs into a single dictionary, with values from the target collection taking precedence.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the collections.</typeparam>
    /// <typeparam name="TValue">The type of the values in the collections.</typeparam>
    /// <param name="source">The source collection of key-value pairs.</param>
    /// <param name="target">The target collection of key-value pairs whose values will override source values for matching keys.</param>
    /// <param name="getKey">Key resolver</param>
    /// <param name="mergeBehavior">Merge behavior</param>
    /// <returns>A new dictionary containing the merged key-value pairs.</returns>
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
        this Dictionary<TKey, TValue> source,
        IEnumerable<TValue> target,
        Func<TValue, TKey> getKey,
        MergeBehavior mergeBehavior
    )
        where TKey : notnull
    {
        if (mergeBehavior is MergeBehavior.KeepTarget)
            foreach (var value in target)
                source[getKey(value)] = value;
        else
            foreach (var value in target)
                source.TryAdd(getKey(value), value);

        return source;
    }
}

/// <summary>
/// Enum defining merge behavior for merging two collections
/// </summary>
public enum MergeBehavior
{
    /// <summary>
    /// Marks that source values are preferred, target values are discarded on conflict
    /// </summary>
    KeepSource,

    /// <summary>
    /// Marks that target values are preferred, source values are discarded on conflict
    /// </summary>
    KeepTarget,
}
