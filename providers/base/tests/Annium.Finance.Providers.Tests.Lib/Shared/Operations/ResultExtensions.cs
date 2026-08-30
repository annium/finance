using Annium.Data.Operations;

namespace Annium.Finance.Providers.Tests.Lib.Shared.Operations;

/// <summary>
/// Bridges plain test domain objects (<see cref="Annium.Finance.Providers.Tests.Lib.User.Order"/>,
/// <see cref="Annium.Finance.Providers.Tests.Lib.User.Position"/>) into the <see cref="IResult{T}"/> pattern
/// the validation extensions they use are built on.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Wraps a value in a successful result.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A successful result carrying the value.</returns>
    public static IResult<T> AsResult<T>(this T value)
    {
        return Result.Create(value);
    }
}
