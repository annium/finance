using System;
using System.Collections.Generic;
using System.Linq;

namespace Annium.Finance.Providers.Abstractions.Domain.Temp;

// TODO: remove temp
public static class EnumExtensions
{
    /// <summary>
    /// Enumerate enum values. Returns single value if enum doesn't have flags attribute
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="value">Value to enumerate.</param>
    /// <returns>Enumerable of values, source value contains.</returns>
    public static IReadOnlyCollection<T> EnumerateFlags<T>(this T value)
        where T : struct, Enum
    {
        return Enum.GetValues<T>().Where(x => value.HasFlag(x)).ToArray();
    }
}
